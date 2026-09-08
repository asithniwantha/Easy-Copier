using System;

namespace Easy_Copier.Models
{
    public record TransferProgress(
        int Percentage,
        double SpeedMegabytesPerSecond,
        TimeSpan EstimatedTimeRemaining,
        string CurrentItemName = "",
        long BytesTransferred = 0,
        long TotalBytesToTransfer = 0);
}
