#pragma strict
var targetAngle: float = 0; // the desired angle
var curAngle: float; // current angle
var accel: float; // applied accel
var angSpeed: float = 0; // current ang speed
var maxAccel: float = 180; // max accel in degrees/second2
var maxASpeed: float = 90; // max angular speed in degrees/second
var pGain: float = 20; // the proportional gain
var dGain: float = 10; // differential gain
private var lastError: float; 

function Start(){
  targetAngle = transform.eulerAngles.y; // get the current angle just for start
  curAngle = targetAngle;
}

function FixedUpdate(){
  var error = targetAngle - curAngle; // generate the error signal
  var diff = (error - lastError)/ Time.deltaTime; // calculate differential error
  lastError = error;
  // calculate the acceleration:
  accel = error * pGain + diff * dGain;
  // limit it to the max acceleration
  accel = Mathf.Clamp(accel, -maxAccel, maxAccel);
  // apply accel to angular speed:
  angSpeed += accel * Time.deltaTime; 
  // limit max angular speed
  angSpeed = Mathf.Clamp(angSpeed, -maxASpeed, maxASpeed);
  curAngle += angSpeed * Time.deltaTime; // apply the rotation to the angle...
  // and make the object follow the angle (must be modulo 360)
  rigidbody.rotation = Quaternion.Euler(0, curAngle%360, 0); 
}