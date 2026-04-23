const { ObjectId } = require('mongodb');

function parseReading(body) {
    const reading = {
        utc: Number(body.utc),
        temperatureC: Number(body.temperatureC),
        humidityPercent: Number(body.humidityPercent),
        eCo2Ppm: Number(body.eCo2Ppm),
        tVocPpb: Number(body.tVocPpb),
        pressureHpa: Number(body.pressureHpa),
        pm25: Number(body.pm25),
        fireAlarm: body.fireAlarm
    }
    
    if(!isFinite(reading.utc)){
        return { error: "Invalid 'utc' field. It must be a number." };
    }

    if(!isFinite(reading.temperatureC)){
        return { error: "Invalid 'temperatureC' field. It must be a number." };
    }
    if(!isFinite(reading.humidityPercent)){
        return { error: "Invalid 'humidityPercent' field. It must be a number." };
    }
    if(!isFinite(reading.eCo2Ppm)){
        return { error: "Invalid 'eCo2Ppm' field. It must be a number." };
    }
    if(!isFinite(reading.tVocPpb)){
        return { error: "Invalid 'tVocPpb' field. It must be a number." };
    }
    if(!isFinite(reading.pressureHpa)){
        return { error: "Invalid 'pressureHpa' field. It must be a number." };
    }
    if(!isFinite(reading.pm25)){
        return { error: "Invalid 'pm25' field. It must be a number." };
    }
    if(typeof reading.fireAlarm !== "boolean"){
        return { error: "Invalid 'fireAlarm' field. It must be a boolean." };
    }

    return reading;
};

function tryParseObjectId(id) {
    try {
        return new ObjectId(id);
    } catch {
        return null;
    }
};

function mapReadingResponse(doc) {
    if(!doc) return null;

    const { _id, ...rest } = doc;
    return { 
        id: _id.toString(),
         ...rest 
        };
}


module.exports = { parseReading, tryParseObjectId, mapReadingResponse };

