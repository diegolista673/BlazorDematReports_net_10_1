function CreateEChartPhaseYearStackedBatch(jsonBatch) {
    var batch = JSON.parse(jsonBatch);
    batch.forEach(item => {
        CreateEChartPhaseYearStacked(item.ElementId,
            JSON.stringify(item.Data));
    });
}

function CreateEChartPhaseLast5YearsBatch(jsonBatch) {
    var batch = JSON.parse(jsonBatch);
    batch.forEach(item => {
        CreateEChartPhaseLast5Years(item.ElementId,
            JSON.stringify(item.Data));
    });
}