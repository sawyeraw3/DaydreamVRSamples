using UnityEngine;
using System.Collections;


/**
 * Sawyer Warden, 2016
 * This script enables teleportation and grab interation with the Daydream View Controller
 * It utilizes material assets and combines dragging methods from the gvr-unity-sdk-master\GoogleVR\DemoScenes\ControllerDemo sample
 * with point-click teleportation concepts from Same Keene @ http://www.sdkboy.com/2016/10/teleportation-vr-google-daydream/
 */

public class ControllerBehavior : MonoBehaviour {

	//The player object (GvrCardboardMain or camera)
	GameObject player;
	//Origin of laser, must have a LineRenderer component
	GameObject barrel;
	//Line to draw from barrel
	LineRenderer laser;
	//Timer for restarting the level when AppButton is held
	float restartTimer = 0f;
	//Time to hold AppButton for restart
	float restartDelay = 3f;
	//GameObject used for navigation/interaction, appears at location of collisions with laser
	public GameObject cursor;
	//The View Controller object, which elbow-rigged
	public GameObject pointerObject;
	//Material used when not hovering over cube
	public Material cubeInactiveMaterial;
	//Material used when hovering over cube
	public Material cubeHoverMaterial;
	//Material used when dragging cube
	public Material cubeActiveMaterial;
	//Max length of LineRenderer
	public float laserDistance = 200f;
	//pointerObject instantiated within the script when teleporting
	//(future updates may outdate this facet
	GameObject localPointerObject;
	//Where the laser is colliding
	Vector3 currentTargetPos;

	// GameObject that is selected
	private GameObject selectedObject;

	// True when selectedObject is being dragged
	private bool dragging;

	// Use this for initialization
	void Start () {
		player = GameObject.FindGameObjectWithTag ("Player");
		//Instatiate the first pointerObject
		localPointerObject = Instantiate (pointerObject, player.transform.position, player.transform.rotation) as GameObject;
		//Identify the barrel
		barrel = GameObject.FindGameObjectWithTag ("Barrel");
		//Initiate the LineRenderer
		setLaser (barrel);
	}
	
	// Update is called once per frame
	private void FixedUpdate() {
		//Determine what's done before the controller connects
		if (GvrController.State != GvrConnectionState.Connected) {
			//Do nothing
		}

		//If dragging, maintain state
		if (dragging) {
			//Check for change in state
			if (!GvrController.ClickButton) {
				EndDragging();
			}
			//Disable the laser when dragging an object
			laser.enabled = false;
		} else {
			//Check for barrel, only draw laser when barrel (and pointerObject) exist
			//	below when teleporting, destruction of the pointerObject makes this necesarry to avoid errors
			barrel = GameObject.FindGameObjectWithTag ("Barrel");
			if(barrel) {
				setLaser (barrel);
			}
			//Determine restart conditions
			if (restartTimer >= restartDelay) {
				Application.LoadLevel (Application.loadedLevel);
			} else if (GvrController.AppButton) {
				restartTimer += Time.deltaTime;
			} else if (GvrController.AppButtonUp) {
				restartTimer = 0f;
			}
			//If the controller is selecting a dragable object, drag it
			if (GvrController.ClickButton && selectedObject != null) {
				StartDragging ();
			} else if (GvrController.ClickButtonUp && currentTargetPos != player.transform.position && selectedObject == null) {
			//Otherwise, if the controller is selecting a non-dragable object and the new position is valid, teleport
				//Determine the controllers current location relative to the player to make reinstatiation/teleportation smoother
				Vector3 localPointerPos = localPointerObject.transform.InverseTransformPoint (player.transform.position);
				//Destroy the instantiated pointerObject to reinstantiate at the selected position
				Destroy (localPointerObject);
				Vector3 teleportPos = new Vector3 (currentTargetPos.x, 1.8f, currentTargetPos.z);
				Vector3 controllerTeleportPos = new Vector3 (teleportPos.x + localPointerPos.x, teleportPos.y + localPointerPos.y, teleportPos.z + localPointerPos.z);
				localPointerObject = Instantiate (pointerObject, controllerTeleportPos, player.transform.rotation) as GameObject;
				player.transform.position = teleportPos;
			}
		}
	}

	//Enable the LineRenderer given an origin containing the component (barrel)
	void setLaser(GameObject startPos) {
		laser = startPos.GetComponent<LineRenderer> ();
		Vector3[] initLaserPositions = new Vector3[ 2 ] { Vector3.zero, Vector3.zero };
		laser.SetPositions( initLaserPositions );
		laser.SetWidth( 0.01f, 0.01f );
		Vector3 v = startPos.transform.forward;
		shootLaser(startPos.transform.position, v, laserDistance);
		laser.enabled = true;
	}

	void shootLaser(Vector3 targetPosition, Vector3 direction, float length) {
		RaycastHit hitInfo;
		if (Physics.Raycast(laser.gameObject.transform.position, direction, out hitInfo)) {
			//Move the cursor to the position of the collision
			cursor.transform.localPosition = hitInfo.point;
			//If hovering over an object tagged Movable, change material and set as selectedObject
			if (hitInfo.collider.gameObject.tag == "Movable") {
				SetSelectedObject (hitInfo.collider.gameObject);
			} else {
			//Otherwise, ground is selected (if there are more objects/tags, add conditionals)
				//Target new position
				currentTargetPos = hitInfo.point;
				//Ensure cubes change material when RaycastHit moves from Movable objects to to other objects
				SetSelectedObject (null);
			}
		} else {
		//If no GameObject hit, reset target position, cursor, and selected object
			currentTargetPos = player.transform.position;
			cursor.transform.localPosition = currentTargetPos;
			SetSelectedObject(null);
		}
		//Update the LineRenderer
		Vector3 endPosition = laser.gameObject.transform.position + ( length * direction );
		laser.SetPosition( 0, laser.gameObject.transform.position );
		laser.SetPosition( 1, endPosition );
	}

	//Changes material of Movable object previously hovered over or being hovered over
	private void SetSelectedObject(GameObject obj) {
		if (selectedObject != null) {
			selectedObject.GetComponent<Renderer>().material = cubeInactiveMaterial;
		}
		if (obj != null) {
			obj.GetComponent<Renderer>().material = cubeHoverMaterial;
		}
		selectedObject = obj;
	}

	//Drag the selectedObject
	private void StartDragging() {
		dragging = true;
		selectedObject.GetComponent<Renderer>().material = cubeActiveMaterial;
		//Cube is reparented to cursor, which gets parented to the barrel. This moves the cube with the pointerObject
		//The redundancy is simply so the cursor still moves with the controlle when laser disabled
		selectedObject.transform.SetParent(cursor.transform, true);
		cursor.transform.SetParent(laser.gameObject.transform, true);
	}

	//Stop dragging selectedObject (opposite of StartDragging()
	//	Conditionals allow calling with a currently null selectedObject
	private void EndDragging() {
		dragging = false;
		if (selectedObject != null) {
			selectedObject.GetComponent<Renderer> ().material = cubeHoverMaterial;
			selectedObject.transform.SetParent (null, true);
		}
		cursor.transform.SetParent(null, true);
	}
}
