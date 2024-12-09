// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

function confirmDelete(url) {
    if (confirm("Are you sure you want to delete this file?")) {
        window.location.href = url;
    }
}







//document.getElementById("uploadForm").onsubmit = function (event) {
//    event.preventDefault(); // Prevent the form from submitting traditionally
//    var fileInput = document.getElementById("fileInput");
//    var file = fileInput.files[0];
//    var chunkSize = 5 * 1024 * 1024; // 5 MB per chunk
//    var totalChunks = Math.ceil(file.size / chunkSize);
//    var currentChunk = 0;
//    var originalFileName = file.name;

//    function uploadChunk() {
//        var start = currentChunk * chunkSize;
//        var end = Math.min(start + chunkSize, file.size);
//        var chunk = file.slice(start, end);

//        var formData = new FormData();
//        formData.append('file', chunk);
//        formData.append('chunkIndex', currentChunk);
//        formData.append('totalChunks', totalChunks);
//        formData.append('originalFileName', originalFileName);

//        var xhr = new XMLHttpRequest();
//        xhr.open('POST', '/FileUpload/Uploads', true);

//        xhr.onload = function () {
//            if (xhr.status === 200) {
//                currentChunk++;
//                if (currentChunk < totalChunks) {
//                    uploadChunk(); // Continue with the next chunk
//                } else {
//                    document.getElementById("success-message").style.display = 'block';
//                    document.getElementById("success-message").innerText = 'File uploaded successfully!';
//                }
//            } else {
//                document.getElementById("error-message").style.display = 'block';
//                document.getElementById("error-message").innerText = 'Error uploading file chunk!';
//            }
//        };

//        xhr.send(formData);
//    }

//    uploadChunk(); // Start uploading the first chunk
//};

