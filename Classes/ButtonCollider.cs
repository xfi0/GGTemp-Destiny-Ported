using MelonLoader;
using Photon.Pun;
using System;
using UnityEngine;
using static GGTemps.Menu.Main;
using static GGTemps.Settings;

namespace GGTemps.Classes
{
	[MelonLoader.RegisterTypeInIl2Cpp]
	public class Button : MonoBehaviour
	{
		public Button(IntPtr ptr) : base(ptr) { }
		public string relatedText;
		public static float buttonCooldown = 0f;

		public void OnTriggerEnter(Collider collider)
		{
			if (Time.time > buttonCooldown && collider == buttonCollider && menu != null)
			{
				buttonCooldown = Time.time + 0.2f;
				GorillaTagger.Instance.StartVibration(rightHanded, GorillaTagger.Instance.tagHapticStrength / 2f, GorillaTagger.Instance.tagHapticDuration / 2f);
                MelonCoroutines.Start(PlaySFX(buttonSfxUrl));
				Toggle(this.relatedText);
			}
		}
		public static string buttonSfxUrl = "https://github.com/okcauq/menusounds/raw/refs/heads/main/sound.mp3";
	}
}

