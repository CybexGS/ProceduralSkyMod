using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralSkyMod
{
	public class SkyCamConstraint : MonoBehaviour
	{
		public Camera main;
		public Camera sky;
		public Camera clear;

		void Update ()
		{
			clear.transform.rotation = sky.transform.rotation = main.transform.rotation;
			clear.fieldOfView = sky.fieldOfView = main.fieldOfView;
		}
	}

	public class PositionConstraint : MonoBehaviour
	{
		public Transform source = null;
		public Transform target = null;

		void Update ()
		{
			if (source == null) return;
			if (target == null) this.transform.position = source.transform.position;
			else target.transform.position = source.transform.position;
		}
	}
}
