using System;

namespace S3Lite
{
    /// <summary>
    /// Settings for routing requests through an intermediate reverse proxy / gateway while the SigV4 signature
    /// stays bound to the upstream endpoint described by <see cref="S3Client.Hostname" />.
    /// Assign to <see cref="S3Client.Gateway" /> (or pass to <see cref="S3Client.WithGateway" />) to enable gateway routing.
    /// <para>
    /// When set, the request is sent to this gateway host with its Host header set to this gateway host. The
    /// gateway is expected to rewrite the Host header to the authority the request was signed for - the upstream
    /// request authority - before forwarding upstream. That signed authority is not necessarily
    /// <see cref="S3Client.Hostname" /> verbatim: for virtual-hosted-style requests it is bucket-prefixed
    /// (for example <c>bucket.s3.us-west-1.example.com</c>), and for path-style requests it is the bare endpoint
    /// (for example <c>s3.us-west-1.example.com</c>), including a non-standard port when one is configured. The
    /// SigV4 signature stays bound to that upstream authority, so it validates once the gateway has restored it.
    /// </para>
    /// <para>
    /// This is NOT a forward/HTTP (CONNECT) proxy. For a forward proxy that preserves the upstream Host
    /// end-to-end, leave this unset and supply an HttpClient configured with a WebProxy instead
    /// (see <see cref="S3Client.WithHttpClient" />).
    /// </para>
    /// </summary>
    public class GatewayConfig
    {
        #region Public-Members

        /// <summary>
        /// Gateway host that requests are sent to. This must be the bare gateway host only (for example
        /// <c>s3.gateway.example.com</c>); supply the protocol and port separately via <see cref="Protocol" />
        /// and <see cref="Port" />.
        /// When null or empty, no gateway routing is performed and requests go directly to <see cref="S3Client.Hostname" />.
        /// </summary>
        public string Hostname { get; set; } = null;

        /// <summary>
        /// Gateway port that requests are sent to.
        /// When 0, the <see cref="S3Client.Port" /> value is used.
        /// </summary>
        public int Port
        {
            get
            {
                return _Port;
            }
            set
            {
                if (value < 0 || value > 65535) throw new ArgumentOutOfRangeException(nameof(Port));
                _Port = value;
            }
        }

        /// <summary>
        /// Protocol (HTTP or HTTPS) used to reach the gateway.
        /// When null, the <see cref="S3Client.Protocol" /> value is used.
        /// </summary>
        public ProtocolEnum? Protocol { get; set; } = null;

        #endregion

        #region Private-Members

        private int _Port = 0;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public GatewayConfig()
        {
        }

        #endregion
    }
}
