using UnityEngine;

namespace ProceduralSkyMod
{
	public static class TransformExtensions
	{
		public static void Reset (this Transform transform)
		{
			transform.position = Vector3.zero;
			transform.rotation = Quaternion.identity;
			transform.localScale = Vector3.one;
		}

		public static void ResetLocal (this Transform transform)
		{
			transform.localPosition = Vector3.zero;
			transform.localRotation = Quaternion.identity;
			transform.localScale = Vector3.one;
		}
	}
}
