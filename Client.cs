using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;
using Fnrp.Fivem.Common.Client;

namespace Binoculars.Client
{
	public class Client : ClientScript
	{
		// Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
		[Tick]
		public async Task OnTick()
		{
			if (this._binocularsActive)
			{
				if (this.CannotDoAction())
				{
					this._binocularsActive = false;
					Screen.ShowNotification("You cannot do this action right now!", false);
					return;
				}
				Ped playerPed = base.LocalPlayer.Character;
				Vehicle playerVeh = playerPed.CurrentVehicle;
				this._fovCurrent = (this.fovMax + this.fovMin) * 0.5f;
				if (!playerPed.IsSittingInVehicle())
				{
					API.TaskStartScenarioInPlace(playerPed.Handle, "WORLD_HUMAN_BINOCULARS", 0, true);
					playerPed.PlayAmbientSpeech("GENERIC_CURSE_MED", "SPEECH_PARAMS_FORCE", 0);
					API.SetTimecycleModifier("default");
					API.SetTimecycleModifierStrength(0.3f);
				}
				await BaseScript.Delay(2000);
				Scaleform cameraScaleform = new Scaleform("BINOCULARS");
				while (!cameraScaleform.IsLoaded)
				{
					await BaseScript.Delay(10);
				}
				Camera camera = new Camera(API.CreateCam("DEFAULT_SCRIPTED_FLY_CAMERA", true));
				camera.AttachTo(playerPed, new Vector3(0f, 0f, 1f));
				camera.Rotation = new Vector3(0f, 0f, playerPed.Heading);
				camera.FieldOfView = this._fovCurrent;
				API.RenderScriptCams(true, false, 0, true, false);
				API.PushScaleformMovieFunction(cameraScaleform.Handle, "SET_CAM_LOGO");
				API.PushScaleformMovieFunctionParameterInt(0);
				API.PopScaleformMovieFunctionVoid();
				BaseScript.TriggerEvent("HideHud", new object[0]);
				BaseScript.TriggerEvent("HideRadar", new object[0]);
				while (this._binocularsActive && playerPed.CurrentVehicle == playerVeh)
				{
					if (Controls.IsControlJustPressed((Control)177, 0) || this.CannotDoAction())
					{
						this._binocularsActive = false;
					}
					this.CheckInputRotation(camera, 1f / (this.fovMax - this.fovMin) * (this._fovCurrent - this.fovMin));
					this.HandleZoom(camera);
					this.HideHUDThisFrame();
					cameraScaleform.Render2D();
					await BaseScript.Delay(0);
				}
				ClientScript.PlayManagedSoundFrontend("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
				playerPed.Task.ClearAll();
				cameraScaleform.Dispose();
				camera.Delete();
				API.RenderScriptCams(false, false, 0, true, false);
				API.ClearTimecycleModifier();
				API.SetNightvision(false);
				API.SetSeethrough(false);
				BaseScript.TriggerEvent("ShowHud", new object[0]);
				BaseScript.TriggerEvent("ShowRadar", new object[0]);
				playerPed = null;
				playerVeh = null;
				cameraScaleform = null;
				camera = null;
			}
			await Task.FromResult<int>(0);
		}

		// Token: 0x06000002 RID: 2 RVA: 0x00002098 File Offset: 0x00000298
		private bool CannotDoAction()
		{
			return Game.PlayerPed.IsDead || Game.PlayerPed.IsCuffed || API.DecorGetBool(Game.PlayerPed.Handle, "IsDead") || API.DecorGetBool(Game.PlayerPed.Handle, "IsGrabbed");
		}

		// Token: 0x06000003 RID: 3 RVA: 0x000020E9 File Offset: 0x000002E9
		[Command("binoculars")]
		internal void OnBinocularsCommand()
		{
			this._binocularsActive = !this._binocularsActive;
		}

		// Token: 0x06000004 RID: 4 RVA: 0x000020FC File Offset: 0x000002FC
		private void CheckInputRotation(Camera camera, float zoomValue)
		{
			float rightAxisX = Game.GetDisabledControlNormal(0, (Control)220);
			float rightAxisY = Game.GetDisabledControlNormal(0, (Control)221);
			Vector3 rotation = camera.Rotation;
			if (rightAxisX != 0f || rightAxisY != 0f)
			{
				float newZ = rotation.Z + rightAxisX * -1f * this.speedX * (zoomValue + 0.1f);
				float newX = Math.Max(Math.Min(20f, rotation.X + rightAxisY * -1f * this.speedY * (zoomValue + 0.1f)), -89.5f);
				camera.Rotation = new Vector3(newX, 0f, newZ);
			}
		}

		// Token: 0x06000005 RID: 5 RVA: 0x0000219C File Offset: 0x0000039C
		public void HandleZoom(Camera camera)
		{
			Game.DisableControlThisFrame(0, (Control)241);
			Game.DisableControlThisFrame(0, (Control)242);
			Game.DisableControlThisFrame(0, (Control)99);
			if (Controls.IsControlPressed((Control)241, 0))
			{
				this._fovCurrent = Math.Max(this._fovCurrent - this.zoomSpeed, this.fovMin);
			}
			else if (Controls.IsControlPressed((Control)242, 0))
			{
				this._fovCurrent = Math.Min(this._fovCurrent + this.zoomSpeed, this.fovMax);
			}
			float setFov = camera.FieldOfView;
			if (Math.Abs(this._fovCurrent - setFov) < 0.1f)
			{
				this._fovCurrent = setFov;
			}
			camera.FieldOfView = this._fovCurrent + (this._fovCurrent - setFov) * 0.05f;
		}

		// Token: 0x06000006 RID: 6 RVA: 0x0000225C File Offset: 0x0000045C
		public void HideHUDThisFrame()
		{
			API.HideHelpTextThisFrame();
			API.HideHudAndRadarThisFrame();
			Screen.Hud.HideComponentThisFrame((HudComponent)1);
			Screen.Hud.HideComponentThisFrame((HudComponent)2);
			Screen.Hud.HideComponentThisFrame((HudComponent)3);
			Screen.Hud.HideComponentThisFrame((HudComponent)4);
			Screen.Hud.HideComponentThisFrame((HudComponent)6);
			Screen.Hud.HideComponentThisFrame((HudComponent)7);
			Screen.Hud.HideComponentThisFrame((HudComponent)8);
			Screen.Hud.HideComponentThisFrame((HudComponent)9);
			Screen.Hud.HideComponentThisFrame((HudComponent)11);
			Screen.Hud.HideComponentThisFrame((HudComponent)12);
			Screen.Hud.HideComponentThisFrame((HudComponent)13);
			Screen.Hud.HideComponentThisFrame((HudComponent)15);
			Screen.Hud.HideComponentThisFrame((HudComponent)18);
			Screen.Hud.HideComponentThisFrame((HudComponent)19);
		}

		// Token: 0x04000001 RID: 1
		private readonly float fovMax = 70f;

		// Token: 0x04000002 RID: 2
		private readonly float fovMin = 5f;

		// Token: 0x04000003 RID: 3
		private readonly float zoomSpeed = 10f;

		// Token: 0x04000004 RID: 4
		private readonly float speedX = 8f;

		// Token: 0x04000005 RID: 5
		private readonly float speedY = 8f;

		// Token: 0x04000006 RID: 6
		private bool _binocularsActive;

		// Token: 0x04000007 RID: 7
		private float _fovCurrent;
	}
}
