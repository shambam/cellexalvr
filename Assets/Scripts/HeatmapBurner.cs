using UnityEngine;

public class HeatmapBurner : MonoBehaviour {

public GameObject firePrefab;
public Material originalMaterial;
public Material transparentMaterial;
private GameObject fire;
private float fadingTime = 0.5f;
private Renderer rend;
private Component[] childrenRenderers;
private bool fadeHeatmap;
private float t = 0;

// Use this for initialization
void Start () {
	rend = GetComponent<Renderer> ();
	childrenRenderers = GetComponentsInChildren<Renderer>();
}

// Update is called once per frame
void Update () {
	if (fadeHeatmap) {
		FadeHeatmap();
	}
}

public void BurnHeatmap() {
	//print ("heatmap grabbed & down pressed!");
	fadeHeatmap = true;
	Vector3 heatmapScale = transform.localScale;
	Vector3 heatmapPosition = transform.position;
	//Vector3 firePosition = new Vector3(heatmapPosition.x, heatmapPosition.y + 2.5f, heatmapPosition.z);
	//fire = Instantiate(firePrefab, heatmapPosition + new Vector3(0, 5 * heatmapScale.z, 0), transform.rotation);
	fire = Instantiate (firePrefab, heatmapPosition + new Vector3(0, 5 * heatmapScale.z, 0), new Quaternion(0, 0, 0, 0));
	fire.transform.localScale = new Vector3(5 * heatmapScale.x, 0.1f, heatmapScale.z);
	//fire.transform.Rotate(new Vector3(180.0f, 0, 0));
	fire.transform.Rotate(new Vector3(270.0f, transform.localEulerAngles.y, 0));
	// fire.SetActive(true);
	this.GetComponents<AudioSource> () [1].PlayDelayed (10000);
}

void FadeHeatmap() {
	//print ("trying to fade out heatmap!");
	//Material heatmapMaterial = rend.material;
	rend.material.Lerp(originalMaterial, transparentMaterial, t);
	foreach (Renderer rend in childrenRenderers) {
		rend.material.Lerp(originalMaterial, transparentMaterial, t);
	}
	t = t + fadingTime * Time.deltaTime;
	if (t >= 1) {
		fadeHeatmap = false;
		Destroy (this.gameObject);
		Destroy (fire);
		t = 0;
	}
	//print (t);
}

}