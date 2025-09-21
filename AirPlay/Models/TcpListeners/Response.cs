using AirPlay.Models.Enums;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AirPlay.Models;

public class Response
{
    private ProtocolType _protocol;
    private StatusCode _statusCode = StatusCode.OK;
    private MemoryStream _responseStream;
    private HeadersCollection _headers;

    public ProtocolType Protocol { get => _protocol; set => _protocol = value; }
    public StatusCode StatusCode { get => _statusCode; set => _statusCode = value; }
    public HeadersCollection Headers => _headers;

    public Response()
    {
        _responseStream = new MemoryStream();
        _headers = new HeadersCollection();
    }

    public Response(ProtocolType protocol) : this()
    {
        _protocol = protocol;
    }

    public Response(ProtocolType protocol, StatusCode statusCode) : this(protocol)
    {
        _statusCode = statusCode;
    }

    public Task WriteAsync(byte[] buffer)
    {
        return WriteAsync(buffer, 0, buffer.Length);
    }

    public Task WriteAsync(byte[] buffer, int index, int count)
    {
        using (var writer = new BinaryWriter(_responseStream, Encoding.ASCII))
        {
            writer.Write(buffer, index, count);
        }

        _headers.Add("Content-Length", count.ToString());

        return Task.CompletedTask;
    }

    public Task<byte[]> ReadAsync()
    {
        return Task.FromResult(_responseStream.ToArray());
    }

    public string GetProtocol() => _protocol switch
    {
        ProtocolType.HTTP10 => "HTTP/1.0",
        ProtocolType.HTTP11 => "HTTP/1.1",
        ProtocolType.RTSP10 => "RTSP/1.0",
        _ => null,
    };
}