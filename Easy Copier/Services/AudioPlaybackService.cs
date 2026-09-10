using System;
using System.IO;
using System.Media;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Easy_Copier.Services
{
    public interface IAudioPlaybackService
    {
        void PlaySuccessSound();
        void PlayFailureSound();
    }

    public class AudioPlaybackService : IAudioPlaybackService
    {
        private readonly ILogger<AudioPlaybackService> _logger;

        public AudioPlaybackService(ILogger<AudioPlaybackService> logger)
        {
            _logger = logger;
        }

        public void PlaySuccessSound()
        {
            PlaySoundFile("Assets/successful_task_completion.wav");
        }

        public void PlayFailureSound()
        {
            PlaySoundFile("Assets/Failed_operation.wav");
        }

        private void PlaySoundFile(string relativePath)
        {
            try
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string fullPath = Path.Combine(basePath, relativePath);

                if (File.Exists(fullPath))
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            using SoundPlayer player = new(fullPath);
                            player.PlaySync();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to play sound: {Path}", fullPath);
                        }
                    });
                }
                else
                {
                    _logger.LogWarning("Sound file not found: {Path}", fullPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating sound playback for {Path}", relativePath);
            }
        }
    }
}
