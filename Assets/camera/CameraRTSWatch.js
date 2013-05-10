/*
 * CameraRTSWatch.js
 * David Shuckerow (djs0017@auburn.edu)
 *
 * Provides a camera that works a lot like the Homeworld camera.
 *
 * 16 October 2012
*/
var target : Transform;
private var distance = 10.0;
var minDistance = 1.0;
var maxDistance = 100.0;

var xSpeed = 250.0;
var ySpeed = 120.0;

var yMinLimit = -90;
var yMaxLimit = 90;

private var x = 0.0;
private var y = 0.0;

@script AddComponentMenu("Camera-Control/RTS Watch")

function Start () {
    var angles = transform.eulerAngles;
    x = angles.y;
    y = angles.x;

	// Make the rigid body not change rotation
   	if (rigidbody)
		rigidbody.freezeRotation = true;
}

function LateUpdate () {
    if (target) {
    	// Rotate
    	if (Input.GetKey("mouse 1") && !Input.GetKey("mouse 0")) {
        	x += Input.GetAxis("Mouse X") * xSpeed * 0.02;
        	y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02;

 			y = ClampAngle(y, yMinLimit, yMaxLimit);
 		}
 		// Zoom
 		if (Input.GetAxis("Mouse ScrollWheel") != 0 || (
 		  Input.GetKey("mouse 1") && Input.GetKey("mouse 0") && Input.GetAxis("Mouse Y") != 0)) {
 		    if (Input.GetAxis("Mouse ScrollWheel") != 0)
 				distance -= Input.GetAxis("Mouse ScrollWheel")*distance/(minDistance)*Time.deltaTime*10;
 			if (Input.GetKey("mouse 1") && Input.GetKey("mouse 0") && Input.GetAxis("Mouse Y") != 0)
 				distance -= Input.GetAxis("Mouse Y")*distance/(minDistance);
 			distance = Mathf.Clamp(distance, minDistance, maxDistance);
 		}
 		
        var rotation = Quaternion.Euler(y, x, 0);
        var position = rotation * Vector3(0.0, 0.0, -distance) + target.position;
        
        transform.rotation = rotation;
        transform.position = position;
    }
}

static function ClampAngle (angle : float, min : float, max : float) {
	if (angle < -360)
		angle += 360;
	if (angle > 360)
		angle -= 360;
	return Mathf.Clamp (angle, min, max);
}