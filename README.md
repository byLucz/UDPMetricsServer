# UDPMetricsServer
Basic realisation of metrics server based on UDP protocol
# Features:
- UDP server listening port **8888**
- Receiving messages in the format `<metric_name>:<value>` (UTF-8)
- Basic validation, names without spaces and colons, numeric values
- Ignoring packets larger than **256 bytes** w/o error
- Storing the latest metric values ​​in dictionary
- Showing all metrics every **5 seconds**
