using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dalamud.Logging;
using Dalamud.Utility;
using ImGuiScene;
using Lumina.Data.Files;

namespace FFLogsViewer.Manager;

public class JobIconsManager : IDisposable
{
    private List<TextureWrap>? jobIcons;
    private volatile bool isLoading;
    private int iconLoadAttemptsLeft = 4;

    public TextureWrap? GetJobIcon(uint jobId)
    {
        if (this.isLoading)
        {
            return null;
        }

        if (this.jobIcons == null)
        {
            this.LoadJobIcons();
        }

        if (this.jobIcons is { Count: 41 } && jobId is >= 0 and <= 40)
        {
            return this.jobIcons[(int)jobId];
        }

        return null;
    }

    public void Dispose()
    {
        this.DisposeIcons();

        GC.SuppressFinalize(this);
    }

    private static TextureWrap? GetIconTextureWrap(int id)
    {
        try
        {
            TexFile? iconTex = null;
            var iconPath = $"ui/icon/062000/0{id}_hr1.tex";
            if (IPC.PenumbraEnabled)
            {
                try
                {
                    iconTex = Service.DataManager.GameData.GetFileFromDisk<TexFile>(IPC.ResolvePenumbraPath(iconPath));
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            iconTex ??= Service.DataManager.GetFile<TexFile>(iconPath);

            if (iconTex != null)
            {
                var tex = Service.Interface.UiBuilder.LoadImageRaw(iconTex.GetRgbaImageData(), iconTex.Header.Width, iconTex.Header.Height, 4);
                if (tex.ImGuiHandle != IntPtr.Zero)
                {
                    return tex;
                }
            }
        }
        catch (Exception e)
        {
            PluginLog.Error(e, "Icon loading failed.");
        }

        return null;
    }

    private void LoadJobIcons()
    {
        if (this.iconLoadAttemptsLeft <= 0)
        {
            return;
        }

        this.jobIcons = new List<TextureWrap>();
        this.isLoading = true;
        var hasFailed = false;

        Task.Run(() =>
        {
            var defaultIcon = GetIconTextureWrap(62143);
            if (defaultIcon != null)
            {
                this.jobIcons.Add(defaultIcon);
            }
            else
            {
                hasFailed = true;
            }

            for (var i = 62101; i <= 62140 && !hasFailed; i++)
            {
                var icon = GetIconTextureWrap(i);
                if (icon != null)
                {
                    this.jobIcons.Add(icon);
                }
                else
                {
                    hasFailed = true;
                }
            }

            if (hasFailed)
            {
                this.DisposeIcons();

                this.jobIcons = null;

                PluginLog.Error($"Job icons loading failed, {--this.iconLoadAttemptsLeft} attempt(s) left.");
            }

            this.isLoading = false;
        });
    }

    private void DisposeIcons()
    {
        if (this.jobIcons != null)
        {
            foreach (var icon in this.jobIcons)
            {
                icon.Dispose();
            }
        }
    }
}
