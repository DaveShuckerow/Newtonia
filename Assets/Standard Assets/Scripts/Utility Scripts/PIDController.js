#pragma strict
 var rightPos : double; // this is where we want it to go. Setpoint, SP
 var wrongPos : double; // this is where it is going. Process Value, PV
 var time : double; // the present time
 var proGain : double; // Proportional gain. We set this.
 var intGain : double; // Integral gain. We set this too.
 var manipVar : double; // The manipulated variable; what we are looking for.
 var error : double; // duh
 
function Start () {

}

function Update () {

}