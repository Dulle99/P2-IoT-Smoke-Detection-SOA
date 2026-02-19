const fs = require('fs');
const path = require('path');
const csv = require('csv-parser');

const { MongoClient } = require('mongodb');

const MONGO_URL = process.env.MONGO_URL || 'mongodb://localhost:27017';
const DB_NAME = process.env.DB_NAME || 'smoke';
const COLLECTION = process.env.COLLECTION || 'readings';

const CSV_PATH = process.env.CSV_PATH || path.resolve(__dirname, "..", "..", "data", "smoke_detection_iot.csv");

//Map CSV row -> MongoDB document
function mapRowToDoc(row) {
    const utc = Number(row["UTC"]);
    const temperatureC = Number(row["Temperature[C]"]);
    const humidityPercent = Number(row["Humidity[%]"]);
    const eCo2Ppm = Number(row["eCO2[ppm]"]);
    const tVocPpb = Number(row["TVOC[ppb]"]);
    const pressureHpa = Number(row["Pressure[hPa]"]);
    const pm25 = Number(row["PM2.5"]);

    const fireAlarm = String(row["Fire Alarm"]).trim() === "1";

    return {
        utc,
        temperatureC,
        humidityPercent,
        eCo2Ppm,
        tVocPpb,
        pressureHpa,
        pm25,
        fireAlarm,
        createdAt: new Date()
    };
}


async function main(){
    console.log("MongoDB URL:", MONGO_URL);
    console.log("Database Name:", DB_NAME);
    console.log("Collection Name:", COLLECTION);
    console.log("CSV Path:", CSV_PATH);

    if(!fs.existsSync(CSV_PATH)){
        console.error("CSV file not found at path:", CSV_PATH);
        process.exit(1);
    }

    const client = new MongoClient(MONGO_URL);
    await client.connect();

    const db = client.db(DB_NAME);
    const collection = db.collection(COLLECTION);

    //indexig
    await collection.createIndex({ utc: 1 });

    let batch = [];
    let insertedCount = 0;
    let processedCount = 0;
    const BATCH_SIZE = 1000;

    const stream = fs.createReadStream(CSV_PATH).pipe(csv());

    for await (const row of stream) {
        processedCount++;

        const doc = mapRowToDoc(row);

        // skip garbage rows where utc is not a number
        if (isNaN(doc.utc))
            continue;

        batch.push(doc);

        if (batch.length >= BATCH_SIZE) {
            const res = await collection.insertMany(batch, { ordered: false });
            insertedCount += res.insertedCount;

            batch = [];

            if (processedCount % (BATCH_SIZE * 5) === 0) {
                console.log(`Processed ${processedCount} rows, inserted ${insertedCount} documents...`);
            }
        }
    }

    if(batch.length > 0){
        const res = await collection.insertMany(batch, { ordered: false});
        insertedCount += res.insertedCount;
    }

    console.log(`Finished processing. Total rows: ${processedCount}, total inserted: ${insertedCount}`);
    await client.close();

}

main().catch(err => {
    console.error("Error during import:", err);
    process.exit(1);
});