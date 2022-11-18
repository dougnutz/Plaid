PlaidUpload = function() {
    var self = this;
    self.Logger = {};

    var chunks=[];
    var chunkSize = 1024 * 1024 * 5;

    // watch the file upload button

    // run the upload
    self.Init = function() {
        self.Log("plaidUpload.Init()");

    };

    self.BeginUpload = function(e,plaid, container) {
        self.Log("starting upload",1);
        // get the file assume one for now
        var file = e[0];
        self.Log("file: " + file.name,1);
        self.Log("container: " + container,1);
        self.Log("plaid: " + plaid,1);
        // check container name
        if (container.length < 1) {
            self.Log("container name too short",1);
            return;
        }
        // validate upload
        self.ValidateUpload(container,plaid, function() {self.UploadFile(file, container, sas)});
    };


    self.ValidateUpload = function(container,plaid, OnSuccess) {
        self.Log("plaidUpload.ValidateUpload()",1);

        var xhr = new XMLHttpRequest();
        // htmlencode the container
        self.Log(encodeURIComponent(container),1);

        var url =plaid+"?token="+ encodeURIComponent(container);
        self.Log("GET url: " + url,1);
        xhr.open("GET", url, true);
        self.Log("sending request",1);
        xhr.onreadystatechange = function() {
            if (xhr.readyState == 4) {
                if (xhr.status == 200) {
                    self.Log("Valid token and container",1);
                    OnSuccess();
                } else {
                    self.Log(xhr.status,1);
                    self.Log("container does not exist or no access",1);
                    return false;
                }
            }
            else if(xhr.readyState == 3) {
                self.Log("xhr state 3",1);
                return false;
            }else if(xhr.readyState == 2) {
                self.Log("xhr state 2",1);
                return false;
            }else if(xhr.readyState == 1) {
                self.Log("xhr state 1",1);
                return false;
            }else if(xhr.readyState == 0) {
                self.Log("xhr state 0",1);
                return false;
            }
        }
        xhr.send();
    }


    self.UploadFile = function(file) {
        // upload smaller files directly to blob storage
        // with small files we can deal with the overhead of the request, options etc
        // TODO we can use a progress event to show the user the upload progress
        // TODO show upload speed along with progress
        self.Log("uploading file",1);

        // Determin number of chunks
        var fileSize = file.size;
        var numChunks = Math.ceil(fileSize / chunkSize);
        self.Log("file size: " + fileSize,1);
        self.Log("num chunks: " + numChunks,1);
        self.Log("chunk size: " + chunkSize,1);

        // Initialize queue with numbers 0 to numChunks
        chunks= Array.from(Array(numChunks).keys());

        // innitially chunks in serial
        for (var i = 0; i < numChunks; i++) {
            //get next chunk
            var chunk = chunks.shift();
            self.UploadChunk(file, chunk);
            self.Log("send chunk: " + chunk,1);
        }

    };
    
    self.UploadChunk = function(file, chunkNum) {
        var xhr = new XMLHttpRequest();
        var url = self.ContainerUrl+  self.Sas;

        xhr.open("PUT", url, false); // false for now to get it working
        xhr.setRequestHeader('x-ms-blob-type', 'BlockBlob');
        // read the chunk

        var start = chunkNum * chunkSize;
        var end = start + chunkSize;
        var blob = file.slice(start, end);
        // send the chunk
        
        xhr.onload = function() {
            if (xhr.status === 200) {
                self.Log("upload complete",1);
            } else {
                self.Log("upload failed",1);
            }
        };
        xhr.send(blob);
    };

    // default to log to console 
    self.Log = function(msg,lvl=0) {
        if (self.Logger.Log) {
            self.Logger.Log(msg,lvl);
            return;
        }

        // default to log to console or alert for level 4
        if(lvl == 4){
            alert(msg);
        }
        
        console.info(msg);
    };
};