using Amazon;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.Runtime;

namespace BulkCloudWatchAlarmsForEc2Instances;

public class AlarmCreator
{
    private readonly BasicAWSCredentials _credentials = new("YOURACCESSKEY", "YOURSECRETKEY");
    private readonly RegionEndpoint _region = RegionEndpoint.USEast1;

    public async Task Run()
    {
        foreach (var instanceId in await GetInstanceIds())
        {
            var metricExists = false;

            var alarmName = $"CPUUtilizationAlarm-{instanceId}";

            using var client = new AmazonCloudWatchClient(_credentials, _region);

            List<DimensionFilter> dimensionFilters = new()
            {
                new DimensionFilter { Name = "InstanceId", Value = instanceId }
            };

            var listMetricsRequest = new ListMetricsRequest
            {
                Dimensions = dimensionFilters,
            };

            var metrics = await client.ListMetricsAsync(listMetricsRequest);

            foreach (var metric in metrics.Metrics)
            {
                var describeAlarmsRequest = new DescribeAlarmsForMetricRequest
                {
                    MetricName = metric.MetricName, Namespace = metric.Namespace, Dimensions = metric.Dimensions
                };

                var alarms = await client.DescribeAlarmsForMetricAsync(describeAlarmsRequest);

                if (alarms.MetricAlarms.Any(x => x.MetricName == "CPUUtilization"))
                {
                    metricExists = true;
                    break;
                }
            }

            if (metricExists)
            {
                Console.WriteLine($"CPUUtilization Alarm for '{instanceId}' already exists. Skipping...");
                continue;
            }

            var request = new PutMetricAlarmRequest
            {
                AlarmName = alarmName,
                ComparisonOperator = ComparisonOperator.GreaterThanOrEqualToThreshold,
                EvaluationPeriods = 1,
                MetricName = "CPUUtilization",
                Namespace = "AWS/EC2",
                Period = 300, // 5 minutes in seconds
                Statistic = Statistic.Average,
                Threshold = 95.0, // Set the threshold to 80%. Adjust as needed.
                ActionsEnabled = true,
                AlarmDescription = "Alarm when CPU exceeds 95%",
                Dimensions = new List<Dimension> { new Dimension { Name = "InstanceId", Value = instanceId } },
                Unit = StandardUnit.Percent
            };

            var response = await client.PutMetricAlarmAsync(request);

            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                Console.WriteLine($"Alarm '{alarmName}' created successfully!");
            }
            else
            {
                Console.WriteLine($"Failed to create alarm. Reason: {response.HttpStatusCode}");
            }
        }
    }

    private async Task<IEnumerable<string>> GetInstanceIds()
    {
        using var client = new AmazonEC2Client(_credentials, new AmazonEC2Config { RegionEndpoint = _region });

        var request = new DescribeInstancesRequest();

        var instances = await client.DescribeInstancesAsync(request);

        // TODO - This can be used to get the instance IDs from a json blob
        //var instances = JsonSerializer.Deserialize<List<InstanceRoot.Root>>(json);
        //var instanceIds = instances!.Where(x => string.IsNullOrEmpty(x.instanceId) == false && x.region == _region.SystemName).Select(x => x.instanceId);

        //return instanceIds.Distinct();

        return instances.Reservations.SelectMany(x => x.Instances).Select(x => x.InstanceId);
    }
}