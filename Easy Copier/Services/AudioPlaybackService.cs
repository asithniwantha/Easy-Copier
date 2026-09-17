using System;
using System.IO;
using System.Media;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Defines operations for playing audio feedback notifications within the application.
    /// </summary>
    public interface IAudioPlaybackService
    {
        /// <summary>
        /// Plays the sound effect designated for successful operation completion.
        /// </summary>
        void PlaySuccessSound();

        /// <summary>
        /// Plays the sound effect designated for failed operations.
        /// </summary>
        void PlayFailureSound();
    }

    /// <summary>
    /// Provides functionality to play audio feedback sound files asynchronously.
    /// </summary>
    public class AudioPlaybackService : IAudioPlaybackService
    {
        /// <summary>
        /// Logger instance used for diagnostic and error logging during audio playback.
        /// </summary>
        private readonly ILogger<AudioPlaybackService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioPlaybackService"/> class with the specified logger.
        /// </summary>
        /// <param name="logger">The logger instance used to record playback warnings or errors.</param>
        public AudioPlaybackService(ILogger<AudioPlaybackService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Plays the sound effect associated with successful operation completion.
        /// </summary>
        public void PlaySuccessSound()
        {
            PlaySoundFile("Assets/successful_task_completion.wav");
        }

        /// <summary>
        /// Plays the sound effect associated with failed operations.
        /// </summary>
        public void PlayFailureSound()
        {
            PlaySoundFile("Assets/Failed_operation.wav");
        }

        /// <summary>
        /// Asynchronously plays an audio WAV file located at the specified relative path within the application directory.
        /// </summary>
        /// <param name="relativePath">The relative file path to the audio file within the application directory.</param>
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
