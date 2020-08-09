using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralSkyMod
{
	class SkyCamConstraint : MonoBehaviour
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
}
