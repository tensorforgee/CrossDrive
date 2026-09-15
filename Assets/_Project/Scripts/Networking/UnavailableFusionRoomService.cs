using System;

namespace CrossDrive.Networking
{
    public sealed class UnavailableFusionRoomService : IRoomTransport
    {
        private readonly NetworkClientView view = new NetworkClientView
        {
            Phase = NetworkGamePhase.Home,
            StatusMessage = "Photon Fusion transport is not configured.",
        };

        public bool IsConfigured => false;
        public string ConfigurationMessage =>
            "Import Photon Fusion 2, create a Fusion AppId, configure Host Mode, and install the CrossDrive Fusion adapter described in README.md.";
        public NetworkClientView View => view;
        public event Action ViewChanged;

        public void CreateRoom(string displayName) => ReportUnavailable();
        public void JoinRoom(string roomCode, string displayName) => ReportUnavailable();
        public void SetReady(bool ready) => ReportUnavailable();
        public void StartMatch() => ReportUnavailable();
        public void SubmitInput(NetworkInputCommand input) { }
        public void RequestRematch() => ReportUnavailable();
        public void LeaveRoom() => ReportUnavailable();

        private void ReportUnavailable()
        {
            view.StatusMessage = ConfigurationMessage;
            ViewChanged?.Invoke();
        }
    }
}
