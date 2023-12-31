namespace FishNet.Alven.SessionManagement
{
    public enum PlayerConnectionState : byte
    {
        /// <summary>
        /// The player has been connected first time.
        /// Is called after the player has been authenticated.
        /// </summary>
        Connected,
        /// <summary>
        /// The player has been reconnected to this session.
        /// Is called after the player has been authenticated.
        /// </summary>
        Reconnected,
        /// <summary>
        /// The player has been permanently disconnected and cannot reconnect to this session.
        /// Next time it will connect as a new player with the same PlayerId.
        /// </summary>
        PermanentlyDisconnected,
        /// <summary>
        /// The player has been temporarily disconnected and can reconnect to this session using their own PlayerId.
        /// </summary>
        TemporarilyDisconnected
    }
}