using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Unity.Mathematics.math;

namespace Unity.Mathematics
{
	// ****************** Player 3D Movement ****************** 

	public class PlayerController : MonoBehaviour
	{
		private DistanceFunctions Df;
		private Vector3 StartPos;
		private Vector3 playerVelocity;
		[SerializeField] private float playerSpeed = 20.0f;
		[SerializeField] private float mouseSpeed = 30.0f;
		[SerializeField] private float wRotYSpeed = 1000;
		[SerializeField] private float wRotXZSpeed = 10;
		[SerializeField] private float wPosSpeed = 0.1f;
		[SerializeField] private Text text;
		[SerializeField] private Button exit;
		[SerializeField] private Button returnToGame;
		[SerializeField] private GameObject panel;
		[SerializeField] private List<Shape4D> objects;

		private RaymarchCam raymarcher;
		private Vector3 deltaMove;
		private Vector2 turn;

		private Shape4D holdingObj = null;

		private bool isMenuOpen = false;

		private void Start()
		{
			turn.x = 90;
			StartPos = transform.position;
			Cursor.lockState = CursorLockMode.Locked;
			raymarcher = Camera.main.GetComponent<RaymarchCam>();
			Df = GetComponent<DistanceFunctions>();
			exit.onClick.AddListener(() =>
			{
				Application.Quit();
			});
			returnToGame.onClick.AddListener(() =>
			{
				changePanel(false);
			});
			resetPosRot();
		}

		private void resetPosRot()
		{
			transform.position = new Vector3(0, 9, 0);
			transform.rotation = new Quaternion(0, 240, 0, 0);
			raymarcher.transform.position = new Vector3(0, 2, 0);
			raymarcher.transform.rotation = new Quaternion(20, -90, 0, 0);
			raymarcher._wPosition = 0;
			raymarcher._wRotation = new Vector3(0, 0, 0);
		}

		// Update is called once per frame
		void Update()
		{
			MovePlayer();
		}
		void changePanel(bool on)
		{
			Cursor.lockState = on ? CursorLockMode.None : CursorLockMode.Locked;
			isMenuOpen = on;
			panel.SetActive(on);
		}

		void selectObject()
		{
			Shape4D obj = GetNearestObjectWithinMaxDist(float.PositiveInfinity);
			if (!obj)
			{
				Debug.Log("object to far away");
			}
			else
			{
				holdingObj = obj;
				holdingObj.transform.SetParent(transform);
			}
		}

		void deselectObject()
		{
			holdingObj.transform.parent = null;
			holdingObj = null;
		}

		void MovePlayer()
		{
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				if (Cursor.lockState == CursorLockMode.None)
				{
					changePanel(false);
				}
				else
				{
					changePanel(true);
				}
			}

			if (isMenuOpen) return;

			if (Input.GetKey(KeyCode.W))
			{
				transform.position += transform.forward * Time.deltaTime * playerSpeed;
			}
			if (Input.GetKey(KeyCode.S))
			{
				transform.position -= transform.forward * Time.deltaTime * playerSpeed;
			}
			if (Input.GetKey(KeyCode.A))
			{
				transform.position -= transform.right * Time.deltaTime * playerSpeed;
			}
			if (Input.GetKey(KeyCode.D))
			{
				transform.position += transform.right * Time.deltaTime * playerSpeed;
			}
			if (Input.GetKey(KeyCode.Space))
			{
				transform.position += transform.up * Time.deltaTime * playerSpeed;
			}
			if (Input.GetKey(KeyCode.LeftControl))
			{
				transform.position -= transform.up * Time.deltaTime * playerSpeed;
			}

			if (Input.GetKey(KeyCode.R))
			{
				resetPosRot();
			}

			if (Input.GetMouseButtonDown(0))
			{
				selectObject();
			}
			if (Input.GetMouseButtonUp(0))
			{
				deselectObject();
			}

			if (!holdingObj)
			{
				var diff = Input.GetAxis("Mouse ScrollWheel") * Time.deltaTime;
				if (Input.GetKey(KeyCode.Alpha1))
				{
					raymarcher._wRotation.x += diff * wRotXZSpeed;
				}
				else if (Input.GetKey(KeyCode.Alpha2))
				{
					raymarcher._wRotation.y += diff * wRotYSpeed;
				}
				else if (Input.GetKey(KeyCode.Alpha3))
				{
					raymarcher._wRotation.z += diff * wRotXZSpeed;
				}

			}

			turn.x += Input.GetAxis("Mouse X") * Time.deltaTime * mouseSpeed;
			turn.y += Input.GetAxis("Mouse Y") * Time.deltaTime * mouseSpeed;
			raymarcher.transform.localRotation = Quaternion.Euler(-turn.y, 0, 0);
			transform.localRotation = Quaternion.Euler(0, turn.x, 0);

			var wChange = Time.deltaTime * wPosSpeed;
			if (Input.GetKey(KeyCode.Q))
			{
				raymarcher._wPosition += wChange;
				if (holdingObj) holdingObj.positionW += wChange;
			}
			if (Input.GetKey(KeyCode.E))
			{
				raymarcher._wPosition -= wChange;
				if (holdingObj) holdingObj.positionW -= wChange;

			}

			text.text = $@"Position:
X: {raymarcher.transform.position.x:F3}
Y: {raymarcher.transform.position.y:F3}
Z: {raymarcher.transform.position.z:F3}
W: {raymarcher._wPosition:F3}

Rotation:
X: {raymarcher.transform.localRotation.eulerAngles.x:F0}
Y: {transform.localRotation.eulerAngles.y:F0}
Z: {transform.localRotation.eulerAngles.z:F0}
WX: {raymarcher._wRotation.x:F3}
WY: {raymarcher._wRotation.y:F3}
WZ: {raymarcher._wRotation.z:F3}
";
		}

		public Shape4D GetNearestObjectWithinMaxDist(float maxDist)
		{
			float minDist = Camera.main.farClipPlane;
			Shape4D curBest = objects[0];

			float4 p4D = float4(transform.position, raymarcher._wPosition);
			Vector3 wRot = raymarcher._wRotation * Mathf.Deg2Rad;

			if ((wRot).magnitude != 0)
			{
				p4D.xw = mul(p4D.xw, float2x2(cos(wRot.x), -sin(wRot.x), sin(wRot.x), cos(wRot.x)));
				p4D.yw = mul(p4D.yw, float2x2(cos(wRot.y), -sin(wRot.y), sin(wRot.y), cos(wRot.y)));
				p4D.zw = mul(p4D.zw, float2x2(cos(wRot.z), -sin(wRot.z), sin(wRot.z), cos(wRot.z)));
			}

			List<Shape4D> allShapes = new List<Shape4D>(FindObjectsOfType<Shape4D>());
			List<Shape4D> shapes = new List<Shape4D>();
			foreach (var s in allShapes)
			{
				if (objects.Contains(s))
				{
					continue;
				}
				shapes.Add(s);
			}
			// var shapes = allShapes.Where(obj => !objects.Contains(obj));
			foreach (var obj in shapes)
			{
				var dist = GetShapeDistance(obj, p4D);
				if (dist < minDist)
				{
					curBest = obj;
					minDist = dist;
				}
			}
			if (maxDist < minDist)
			{
				return null;
			}
			Debug.Log($"Holding object {curBest.name} with distance {minDist}");

			return curBest;
		}
		public float GetShapeDistance(Shape4D shape, float4 p4D)
		{
			p4D -= (float4)shape.Position();

			p4D.xz = mul(p4D.xz, float2x2(cos(shape.Rotation().y), sin(shape.Rotation().y), -sin(shape.Rotation().y), cos(shape.Rotation().y)));
			p4D.yz = mul(p4D.yz, float2x2(cos(shape.Rotation().x), -sin(shape.Rotation().x), sin(shape.Rotation().x), cos(shape.Rotation().x)));
			p4D.xy = mul(p4D.xy, float2x2(cos(shape.Rotation().z), -sin(shape.Rotation().z), sin(shape.Rotation().z), cos(shape.Rotation().z)));

			p4D.xw = mul(p4D.xw, float2x2(cos(shape.RotationW().x), sin(shape.RotationW().x), -sin(shape.RotationW().x), cos(shape.RotationW().x)));
			p4D.zw = mul(p4D.zw, float2x2(cos(shape.RotationW().z), -sin(shape.RotationW().z), sin(shape.RotationW().z), cos(shape.RotationW().z)));
			p4D.yw = mul(p4D.yw, float2x2(cos(shape.RotationW().y), -sin(shape.RotationW().y), sin(shape.RotationW().y), cos(shape.RotationW().y)));



			switch (shape.shapeType)
			{
				case Shape4D.ShapeType.HyperCube:
					return Df.sdHypercube(p4D, shape.Scale());

				case Shape4D.ShapeType.HyperSphere:
					return Df.sdHypersphere(p4D, shape.Scale().x);

				case Shape4D.ShapeType.DuoCylinder:
					return Df.sdDuoCylinder(p4D, ((float4)shape.Scale()).xy);
				case Shape4D.ShapeType.plane:
					return Df.sdPlane(p4D, shape.Scale());
				case Shape4D.ShapeType.Cone:
					return Df.sdCone(p4D, shape.Scale());
				case Shape4D.ShapeType.FiveCell:
					return Df.sd5Cell(p4D, shape.Scale());
				case Shape4D.ShapeType.SixteenCell:
					return Df.sd16Cell(p4D, shape.Scale().x);

			}

			return Camera.main.farClipPlane;
		}
	}
}