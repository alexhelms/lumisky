using Flurl;
using Flurl.Http;
using LumiSky.Core.Profile;
using System.Text.Json.Serialization;

namespace LumiSky.Core.Services;

// NOTE: This interface is intended to demonstrate that more mount position providers
//       may be added in the future. For now this implementation is specific for
//       SDSO's dashborad that shows positions of multiple mounts. The prometheus
//       metrics are from the nina prometheus exporter plugin at:
//       https://github.com/alexhelms/nina-prometheus-exporter

public interface IMountPositionProvider
{
    string Name { get; }

    Task<List<MountPosition>> GetMountPositions();
}

public class PrometheusMountPosition : IMountPositionProvider
{
    private readonly IProfileProvider _profileProvider;

    public string Name => "Prometheus";

    public PrometheusMountPosition(IProfileProvider profileProvider)
    {
        _profileProvider = profileProvider;
    }

    public async Task<List<MountPosition>> GetMountPositions()
    {
        try
        {
            var baseUrl = _profileProvider.Current.Processing.PrometheusMountPositionUrl ?? string.Empty;

            var altTask = new Url(baseUrl)
                .AppendPathSegments("api", "v1", "query")
                .AppendQueryParam("query", "nina_mount_alt")
                .WithTimeout(TimeSpan.FromSeconds(3))
                .GetJsonAsync<RootObject>();

            var azTask = new Url(baseUrl)
                .AppendPathSegments("api", "v1", "query")
                .AppendQueryParam("query", "nina_mount_az")
                .WithTimeout(TimeSpan.FromSeconds(3))
                .GetJsonAsync<RootObject>();

            await Task.WhenAll(altTask, azTask);

            var altValues = GetMetricValues(altTask.Result);
            var azValues = GetMetricValues(azTask.Result);

            var names = altValues.Keys.Concat(azValues.Keys).ToHashSet();
            var positions = new List<MountPosition>();
            foreach (var name in names)
            {
                double alt = altValues.GetValueOrDefault(name);
                double az = azValues.GetValueOrDefault(name);
                positions.Add(new MountPosition(name, alt, az));
            }

            return positions;
        }
        catch (FlurlHttpException ex)
        {
            Log.Error(ex, "Error getting mount positions from prometheus");
            return [];
        }

        static Dictionary<string, double> GetMetricValues(RootObject obj)
        {
            if (obj.Status != "success") return [];
            if (obj.Data is null) return [];
            if (obj.Data.ResultType != "vector") return [];

            var items = new Dictionary<string, double>();

            foreach (var result in obj.Data.Result)
            {
                string name = result.Metric.Hostname;
                double value = 0;

                if (result.Value.Count == 2)
                {
                    double.TryParse(result.Value[1].ToString(), out value);
                }

                items[name] = value;
            }

            return items;
        }
    }

    private class RootObject
    {
        public string Status { get; set; } = null!;
        public Data? Data { get; set; }
    }

    private class Data
    {
        public string ResultType { get; set; } = null!;
        public List<Result> Result { get; set; } = [];
    }

    private class Result
    {
        public Metric Metric { get; set; } = null!;
        public List<object> Value { get; set; } = [];
    }

    private class Metric
    {
        [JsonPropertyName("__name__")]
        public string Name { get; set; } = null!;

        public string Hostname { get; set; } = null!;

        public string Instance { get; set; } = null!;

        public string Job { get; set; } = null!;

        [JsonPropertyName("mount_name")]
        public string MountName { get; set; } = null!;

        public string Profile { get; set; } = null!;
    }
}

public record MountPosition(string Name, double Altitude, double Azimuth);



