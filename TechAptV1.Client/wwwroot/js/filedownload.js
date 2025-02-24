window.triggerFileDownload = (fileName, base64Data, contentType) => {
    try {
        const byteCharacters = atob(base64Data);
        const byteNumbers = new Array(byteCharacters.length);
        for (let i = 0; i < byteCharacters.length; i++) {
            byteNumbers[i] = byteCharacters.charCodeAt(i);
        }
        const byteArray = new Uint8Array(byteNumbers);
        const blob = new Blob([byteArray], { type: contentType });

        const link = document.createElement("a");
        link.href = URL.createObjectURL(blob);
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);

        console.log("File download triggered successfully.");
    } catch (err) {
        console.error("Error in triggerFileDownload:", err);
    }
};

//if you want to download file from url of the api so that it reduce cpu usage
window.triggerFileDownloadFromUrl = (fileUrl) => {
    try {
        const link = document.createElement("a");
        link.href = fileUrl;
        link.download = "NumbersData.xml";
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    } catch (error) {
        console.error("Error in triggerFileDownloadFromUrl:", error);
    }
};

window.triggerFileDownloadBase64Data = (fileName, base64Data, mimeType) => {
    try {
        const link = document.createElement("a");
        link.href = `data:${mimeType};base64,${base64Data}`;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    } catch (error) {
        console.error("Error in triggerFileDownloadFromUrl:", error);
    }
};
