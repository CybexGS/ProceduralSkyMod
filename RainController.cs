using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralSkyMod
{
	public class RainController
	{
		public static ParticleSystem[] RainParticleSystems { get; set; }
		public static AudioSource RainAudio { get; set; }
		public static float VolumeMultiplier { get; set; }

		private static int[] maxParticleEmission;
		private static ParticleSystem.ShapeModule shapeModule;

		public static void SetRainParticleSystemArray (ParticleSystem[] systems)
		{
			RainParticleSystems = new ParticleSystem[3];
			maxParticleEmission = new int[3];

			for (int i = 0; i < systems.Length; i++)
			{
				if (systems[i].gameObject.name.Contains("RainDrop"))
				{
					RainParticleSystems[0] = systems[i];
					maxParticleEmission[0] = (int)systems[i].emission.rateOverTime.constant;
					RainParticleSystems[0].Play();
				}
				else if (systems[i].gameObject.name.Contains("RainCluster"))
				{
					RainParticleSystems[1] = systems[i];
					maxParticleEmission[1] = (int)systems[i].emission.rateOverTime.constant;
					RainParticleSystems[1].Play();
				}
				else if (systems[i].gameObject.name.Contains("RainHaze"))
				{
					RainParticleSystems[2] = systems[i];
					maxParticleEmission[2] = (int)systems[i].emission.rateOverTime.constant;
					RainParticleSystems[2].Play();
				}
				else Debug.LogWarning(string.Format("ProSkyMod RainParticleController ERR-00: No name match for {0}", RainParticleSystems[i].gameObject.name));
			}
		}

		public static void SetShapeTextures ()
		{
			shapeModule = RainParticleSystems[0].shape;
			shapeModule.texture = WeatherSource.CloudRenderImage0;
			shapeModule = RainParticleSystems[1].shape;
			shapeModule.texture = WeatherSource.CloudRenderImage1;
			shapeModule = RainParticleSystems[2].shape;
			shapeModule.texture = WeatherSource.CloudRenderImage2;
		}

		public static void SetRainStrength (float strength)
		{
			ParticleSystem.EmissionModule module = RainParticleSystems[0].emission;
			module.rateOverTime = maxParticleEmission[0] * strength;

			module = RainParticleSystems[1].emission;
			module.rateOverTime = maxParticleEmission[1] * strength;

			module = RainParticleSystems[2].emission;
			module.rateOverTime = maxParticleEmission[2] * strength;

			if (RainAudio.isPlaying)
			{
				if (strength > 0)
				{
					float multiplier = 0;
					int samples = 0;
					for (int x = 0; x < WeatherSource.CloudRenderImage0.width; x++)
					{
						for (int y = 0; y < WeatherSource.CloudRenderImage0.height; y++)
						{
							multiplier += Mathf.RoundToInt(WeatherSource.CloudRenderImage0.GetPixel(x, y).a);
							samples++;
						}
					}
					multiplier /= samples; // percentage of sky directly above player that is raining
					multiplier = SimplisticCompressor(multiplier, 0.125f);
					//multiplier = SimplisticCompressor(multiplier, 0.25f);
					RainAudio.volume = Mathf.MoveTowards(RainAudio.volume, strength * multiplier, 0.001f);
				}
				else RainAudio.Stop();
			}
			else
			{
				if (strength > 0) RainAudio.Play();
			}
		}

		public static void SetRainColor (Color color)
		{

			color.a = 0.3f;
			Material m = RainParticleSystems[0].GetComponent<ParticleSystemRenderer>().sharedMaterial;
			m.SetColor("_Color", color);

			color.a = 0.5f;
			m = RainParticleSystems[1].GetComponent<ParticleSystemRenderer>().sharedMaterial;
			m.SetColor("_Color", color);
			m = RainParticleSystems[2].GetComponent<ParticleSystemRenderer>().sharedMaterial;
			m.SetColor("_Color", color);
		}

		private static float SimplisticCompressor(float value, float gain)
        {
			gain = Mathf.Clamp01(gain);
			return value * (1 - gain) + gain;
        }
	}
}
