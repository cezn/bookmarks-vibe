#! /bin/bash

curl -XPOST -H 'content-type: application/json'   http://localhost:5172/api/summarize --data '{
 "url": "https://devblogs.microsoft.com/dotnet/ai-vector-data-dotnet-extensions-ga/",
 "suggestedTags": ["dotnet", "cooking", "ai", "finance"]
}'   | jq
