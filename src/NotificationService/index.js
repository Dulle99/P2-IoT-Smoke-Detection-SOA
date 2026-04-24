const path = require("path");
const grpc = require("@grpc/grpc-js");
const protoLoader = require("@grpc/proto-loader");
const mqtt = require("mqtt");

// Load the gRPC proto file
const PROTO_PATH = path.join(__dirname, "notification.proto");

// MQTT configuration
const MQTT_BROKER_URL = process.env.MQTT_BROKER_URL || "mqtt://localhost:1883";
const MQTT_TOPIC_NOTIFICATIONS = process.env.MQTT_TOPIC_NOTIFICATIONS || "iot/pm25/notifications";

const mqttClient = mqtt.connect(MQTT_BROKER_URL);

mqttClient.on("connect", () => {
    console.log(`Connected to MQTT broker at ${MQTT_BROKER_URL}`);
});
mqttClient.on("error", (err) => {
    console.error("Failed to connect to MQTT broker:", err);
});


const packageDefinition = protoLoader.loadSync(PROTO_PATH, {
  keepCase: true,
  longs: String,
  enums: String,
  defaults: true,
    oneofs: true,
});

const notificationProto = grpc.loadPackageDefinition(packageDefinition).notification;

function sendPm25Alert(call, callback) {
    const req = call.request;
    console.log("Received pm25 alert via gRPC:", req);

    const notification = {
        type: "PM25_ALERT",
        deviceId: req.deviceId,
        pm25: req.pm25,
        temperature: req.temperature,
        timestampUtc: req.timestampUtc,
        message: `PM25 alert from device ${req.deviceId} detected PM25 level ${req.pm25} at ${new Date(req.timestampUtc).toISOString()}`,
    };

    mqttClient.publish(MQTT_TOPIC_NOTIFICATIONS, JSON.stringify(notification), { qos: 1 }, (err) => {
        if(err) {
            console.error("Failed to publish MQTT message:", err);
            callback(null, { status: "FAILED_TO_PUBLISH_NOTIFICATION" });
            return;
        }

        console.log("Published PM25 alert to MQTT topic:", MQTT_TOPIC_NOTIFICATIONS);
        callback(null, { status: "OK" });
    });
}

function main() {
    const server = new grpc.Server();

    server.addService(notificationProto.NotificationService.service, {
        sendPm25Alert: sendPm25Alert,
    });

    const addr = "0.0.0.0:3000";
    server.bindAsync(addr, grpc.ServerCredentials.createInsecure(), (err, port) => {
        if(err) {
            console.error("Failed to bind gRPC server:", err);
            return;
        }
        console.log(`gRPC server is running on ${addr}`);
        server.start();
    });
}

main();