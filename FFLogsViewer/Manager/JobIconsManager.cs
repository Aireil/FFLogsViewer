using System.Collections.Generic;
using System.Threading.Tasks;
using Dalamud.Interface.Internal;

namespace FFLogsViewer.Manager;

public class JobIconsManager
{
    private List<IDalamudTextureWrap>? jobIcons;
    private volatile bool isLoading;
    private int iconLoadAttemptsLeft = 4;

    public IDalamudTextureWrap? GetJobIcon(uint jobId)
    {
        if (this.isLoading)
        {
            return null;
        }

        if (this.jobIcons == null)
        {
            this.LoadJobIcons();
        }

        if (this.jobIcons is { Count: 41 } && jobId <= 40)
        {
            return this.jobIcons[(int)jobId];
        }

        return null;
    }

    private void LoadJobIcons()
    {
        if (this.iconLoadAttemptsLeft <= 0)
        {
            return;
        }

        this.jobIcons = new List<IDalamudTextureWrap>();
        this.isLoading = true;
        var hasFailed = false;

        Task.Run(() =>
        {
            var defaultIcon = Service.TextureProvider.GetIcon(62143);
            if (defaultIcon != null)
            {
                this.jobIcons.Add(defaultIcon);
            }
            else
            {
                hasFailed = true;
            }

            for (uint i = 62101; i <= 62140 && !hasFailed; i++)
            {
                var icon = Service.TextureProvider.GetIcon(i);
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
                this.jobIcons = null;

                Service.PluginLog.Error($"Job icons loading failed, {--this.iconLoadAttemptsLeft} attempt(s) left.");
            }

            this.isLoading = false;
        });
    }
}
