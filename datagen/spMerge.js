function spMerge(id, newReadingJson) {
    var collection = getContext().getCollection();

    // Query documents and take 1st item.
    var isAccepted = collection.queryDocuments(
        collection.getSelfLink(),
        'SELECT * FROM root r where r.id="' + id + '"',
    function (err, feed, options) {
        if (err) throw err;

        // Check the feed and if empty, set the body to 'no docs found', 
        // else take 1st element from feed
        if (!feed || !feed.length) {
            var response = getContext().getResponse();
            response.setBody('no docs found');
        }
        else {
            var response = getContext().getResponse();
            var existing = feed[0];

            var newReading = JSON.parse(newReadingJson);
            existing["p1"] = newReading["p1"].concat(existing["p1"]);
            existing["p2"] = newReading["p2"].concat(existing["p2"]);

            collection.replaceDocument(existing._self, existing,
                function(err, itemReplaced) {
                    if (err) throw err;

                    response.setBody(JSON.stringify(itemReplaced));
                });
        }
    });

    if (!isAccepted) throw new Error('The query was not accepted by the server.');
}