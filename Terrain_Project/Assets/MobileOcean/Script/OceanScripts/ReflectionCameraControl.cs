using UnityEngine;
using System.Collections;
public class ReflectionCameraControl : MonoBehaviour {

	void OnPreRender(){
		GL.SetRevertBackfacing (true);
	}
	
	void OnPostRender() {
		GetComponent<Camera>().targetTexture = MirrorReflection.m_ReflectionTexture;
		GL.SetRevertBackfacing (false);
	}

	void OnDestroy(){
		GL.SetRevertBackfacing (false);
	}
}
