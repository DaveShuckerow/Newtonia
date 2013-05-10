#pragma strict
var target : GameObject;

function Start () {

}

function LateUpdate () {
	if (target != null) {
		transform.position = target.transform.position;
		transform.rotation = target.transform.rotation;
	}
}