namespace Canopy.Api.Client
{
    public interface IConnectionManager
    {
        ConnectionInformation Connection { get; }

        void ClearConnectionInformation();
        bool LoadConnectionInformation();
        void SetConnectionInformation(ConnectionInformation connection);
    }
}
