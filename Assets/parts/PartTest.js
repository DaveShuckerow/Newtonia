#pragma strict
import PartLoader;
import ShipBehavior;
//var part1 : GameObject;
//var part2 : GameObject;
function Start () {
    PartLoader.PreloadParts();
	var part1 = PartLoader.LoadPart("Cockpit1");
	var part2 = PartLoader.LoadPart("Engine1");
	//var part3 = PartLoader.LoadPart("Engine1");
	//var part4 = PartLoader.LoadPart("Engine1");
	//var part5 = PartLoader.LoadPart("Engine1");
	part1.transform.parent = transform;
	part2.transform.parent = transform;
	//part3.transform.parent = transform;
	//part4.transform.parent = transform;
	//part5.transform.parent = transform;
	var ship : ShipBehavior = GetComponent("ShipBehavior") as ShipBehavior;
	ship.Initialize();
	(part1.GetComponent(PartBehavior) as PartBehavior).ConnectToHardpoint((part2.GetComponent(PartBehavior) as PartBehavior),0,0);
	//(part3.GetComponent("PartBehavior") as PartBehavior).ConnectToHardpoint((part2.GetComponent("PartBehavior") as PartBehavior),1,2);
	//(part4.GetComponent("PartBehavior") as PartBehavior).ConnectToHardpoint((part2.GetComponent("PartBehavior") as PartBehavior),2,1);
	//(part5.GetComponent("PartBehavior") as PartBehavior).ConnectToHardpoint((part2.GetComponent("PartBehavior") as PartBehavior),3,4);
	
}