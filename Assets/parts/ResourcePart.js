#pragma strict

var resources : ResourceDeposit[];
var range : double;
var capacity : double;
var rate : double;
var resTypes : Resource;

// Flags
var doResource : boolean;
var resTarget : ResourceDeposit;

function Start () {

}

function Update () {

}

function FixedUpdate() {
	
}

function MineResources(deposit : ResourceDeposit) {
	if (Vector3.Magnitude(deposit.transform.position - transform.position) < range) {
		// We can mine.
		// First check for the resources being present.
		for (var r in resources) {
			if (r.type == deposit.type) {
				r.amount += Mathf.Min(deposit.amount,rate);
				deposit.amount -= Mathf.Min(deposit.amount,rate);
				return;
			}
		}
		// It's not, so add it.
		var d = new ResourceDeposit();
		var a = new Array(resources);
		d.type = deposit.type;
		d.amount = Mathf.Min(deposit.amount,rate);
		deposit.amount -= Mathf.Min(deposit.amount,rate);
		a.Push(d);
		resources = a.ToBuiltin(ResourceDeposit);
	}
}