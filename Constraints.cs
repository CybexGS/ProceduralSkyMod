using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

// OnPreCull used to override VR HMD tracking
// https://forum.unity.com/threads/how-to-disable-hmd-movement-for-second-camera.482468/#post-4282909
namespace ProceduralSkyMod
{
	public class SkyCamConstraint : MonoBehaviour
	{
		public Camera main;
		public Camera sky;
		public Camera clear;

		void OnPreCull()
		{
			clear.transform.rotation = sky.transform.rotation = main.transform.rotation;
			clear.fieldOfView = sky.fieldOfView = main.fieldOfView;
		}
	}

	public class PositionConstraint : MonoBehaviour
	{
		public Transform source = null;
		public Transform target = null;

		void OnPreCull ()
		{
			if (source == null) return;
			if (target == null) transform.position = source.transform.position;
			else target.transform.position = source.transform.position;
		}
	}
}
