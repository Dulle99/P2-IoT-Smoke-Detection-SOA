const fs = require('fs');
const path = require('path');
const yaml = require('js-yaml');
const swaggerUi = require('swagger-ui-express');
const express = require('express');
const {connectDb, getDb} = require('./db');
const {ObjectId} = require('mongodb');
const { parseReading, tryParseObjectId, mapReadingResponse } = require('./utilities');

const app = express();
app.use(express.json());

// Load OpenAPI spec and set up Swagger UI
// http://localhost:5001/api-docs
// http://localhost:5001/api-docs.json
const openApiPath = path.join(__dirname, '..', 'openapi.yaml');
const swaggerDocument = yaml.load(fs.readFileSync(openApiPath, 'utf8'));

app.use('/api-docs', swaggerUi.serve, swaggerUi.setup(swaggerDocument));
app.get('/api-docs.json', (req, res) => {
    res.json(swaggerDocument);
});

//#region Checking DB connection

app.get('/health', async (req, res) => {
    try{
        const db = getDb();
        await db.command({ ping: 1 });
        res.json({ status: 'Ok', service: "DataService", mongo: 'Connected' });
    }
    catch{
        res.status(500).json({ status: 'Error', service: "DataService", mongo: 'Not Connected' });
    }
});

//#endregion Checking DB connection

//#region Post

app.post("/readings", async (req, res) => {
    const parsed = parseReading(req.body);

    if(parsed.error){
        return res.status(400).json({ error: parsed.error  });
    }

    const db = getDb();
    const collection = db.collection('readings');

    const doc = {
        ...parsed,
        createdAt: new Date() 
    };
    
    const result = await collection.insertOne(doc);

    res.status(201).json(mapReadingResponse({ _id: result.insertedId, ...doc }));
});

//#endregion Post

//#region Get

// Read all
app.get("/readings", async (req, res) => {

    const db = getDb();
    const collection = db.collection('readings');

    // Allow an optional `limit` if limit is not provided or invalid, default to 50
    let limit = 50;
    const parsed = parseInt(req.query.limit, 10);

    if (req.query.limit && !isNaN(parsed) && parsed > 0) {
        limit = parsed;
    }

    const items = await collection.find().sort({ utc: -1 }).limit(limit).toArray();

    res.json(items.map(mapReadingResponse));
});

// Read by ID

app.get("/readings/:id",async (req, res) => {

    const objectId = tryParseObjectId(req.params.id);

    if(!objectId){
        return res.status(400).json({ error: "Invalid ID format. Must be a valid ObjectId." });
    }

    const db = getDb();
    const collection = db.collection('readings');

    const item = await collection.findOne({ _id: objectId });
    if(!item){
        return res.status(404).json({ error: "Reading not found with the provided ID." });
    }
    res.json(mapReadingResponse(item));
});

//#endregion Get

//#region Put

app.put('/readings/:id', async (req, res) => {
    const objectId = tryParseObjectId(req.params.id);
    if(!objectId){
        return res.status(400).json({ error: "Invalid ID format. Must be a valid ObjectId." });
    }

    const parsed = parseReading(req.body);
    if(parsed.error){
        return res.status(400).json({ error: parsed.error  });
    }

    const db= getDb();
    const collection = db.collection('readings');

    const existing = await collection.findOne({ _id: objectId });
    if(!existing){
        return res.status(404).json({ error: "Reading not found with the provided ID." });
    }

    const updatedDoc = {
        ...parsed,
        createdAt: existing.createdAt ?? new Date(),
        updatedAt: new Date()
    };

    await collection.updateOne(
        { _id: objectId },
        { $set: updatedDoc });

    const updatedItem = await collection.findOne({ _id: objectId });
    res.json(mapReadingResponse(updatedItem));
});

//#endregion Put

//#region Delete

app.delete('/readings/:id', async (req, res) => {
    const objectId = tryParseObjectId(req.params.id);
    if(!objectId){
        return res.status(400).json({ error: "Invalid ID format. Must be a valid ObjectId." });
    }

    const db= getDb();
    const collection = db.collection('readings');

    const existing = await collection.findOne({ _id: objectId });
    if(!existing){
        return res.status(404).json({ error: "Reading not found with the provided ID." });
    }

    await collection.deleteOne({ _id: objectId });
    res.json({ message: "Reading deleted successfully." });
});

//#endregion Delete

const PORT = process.env.PORT || 5001;

connectDb()
.then(() => {
    app.listen(PORT, () =>{
        console.log(`Data Service is running on port ${PORT}`);
    });
})
.catch((err) => {
    console.error("Failed to connect to MongoDB:", err);
    process.exit(1);
});



