# dotnetcore-cosmos-changefeed

This sample generates json records in the following format to be stored in Azure Cosmos DB. When a new record with the same ID is generated, its value is merged with the existing record in Cosmos.  Additionally through Cosmos DB change feed, any insert or update is persisted to Azure Data Lake Storage (ADLS) Gen2. 

```json
{
    "tag": "000tag",
    "id": "00000000-0000-0000-0000-000000000000_000tag_1548979200",
    "t": 1548979200,
    "p1": [
        {"d": 0.1, "l": 1548979200, "s": "raw"}
    ],
    "p2": [
        {"d": 0.1, "l": 1548979200, "s": "raw"}
    ]
}
```

When a new record with the same ID is generated, the two records are merged, with the latest data in the beginning of the arrays:

```json
{
    "tag": "000tag",
    "id": "00000000-0000-0000-0000-000000000000_000tag_1548979200",
    "t": 1548980000,
    "p1": [
        {"d": 0.2, "l": 1548979200, "s": "verified"}
        {"d": 0.1, "l": 1548979200, "s": "raw"}
    ],
    "p2": [
        {"d": 0.2, "l": 1548979200, "s": "verified"}
        {"d": 0.1, "l": 1548979200, "s": "raw"}
    ]
}
```

## About dotnet core development with Azure 
This is also an exercise for me as a new dotnet core developer to learn to implement the following:
1. Store Cosmos DB key in Azure Key Vault
2. Use Azure Managed Service Identity (MSI) to access Azure Key Vault and ADLS Gen2, both in Azure and on dev machine
3. Send logs to Azure Application Insights
4. Take configuration from either environment variables or configuration files 
5. Run code analysis with FxCopAnalyzers
6. Unit tests with XUnit and Moq
7. Build and test with Github actions

## About the use case
1. Each record in Cosmos DB is stored as a file in storage.  This could result in a huge number of small files, making it inefficient for data analysis engines such as Spark to work with the files.  To work around this in real world scenarios, once we know the records won't be updated any more, run jobs to compact small files. 
2. There's no transactional consistency between the updates in Cosmos DB and in ADLS Gen2. If an update suceeds in Cosmos DB but fails in ADLS Gen2, data could drift. A more robust error handling logic may need to be implemented in real world scenarios. Cosmos DB also has a new feature called Analytical Storage in preview. It can potentially replace the functionality of change feed in this case.
3. There are two approaches to merge the records as listed below. Turns out the first approach takes about 12 RUs per record, and the second, 15 RUs in this sample. This is expected because the second approach does computation inside Cosmos DB. In real world scenarios, further tests need to be done to determine on a suitable approach, taking into consideration cost, latency, and throughput.
    * do a point read from Cosmos DB to retrieve the existing record, merge the record in the application, and write the record back to Cosmos
    * run a stored procedure to update a record
4. Azure.Cosmos 4.0 is used for data generation, but it doesn't support change feed yet, so Microsoft.Azure.Cosmos 3.5 is used with change feed.

