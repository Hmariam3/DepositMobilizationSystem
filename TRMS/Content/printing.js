function printDiv() {
    try {
        // Get the content to print
        var printContents = document.getElementById("printArea").innerHTML;

        if (!printContents) {
            console.error("No content found in the element with ID 'printArea'.");
            alert("Content to print is missing.");
            return;
        }

        // Open a new window for printing
        var printWindow = window.open('', '_blank');
        if (!printWindow) {
            alert("Unable to open a new window for printing. Please check your browser's popup settings.");
            return;
        }

        // Write the HTML structure and content
        printWindow.document.write(`
            <html>
            <head>
                <title>Print</title>
                <style>
                    /* Ensure A4 size for printing */
                    @page {
                        size: A4;
                        margin: 20mm; /* Adjust margins as needed */
                    }

                    /* Apply styles only during print */
                    @media print {
                        body {
                            font-family: Arial, sans-serif;
                            margin: 0;
                            padding: 0;
                            width: 100%;
                        }

                        .custom-logo {
                            width: 102px;
                            height: 80px;
                        }

                        .print-container {
                            width: 100%;
                            max-width: 100%;
                            margin: auto;
                        }

                        .agndatable {
                            width: 100%;
                            border-collapse: collapse;
                        }

                        .agndatable th, .agndatable td {
                            border: 1px solid black;
                            padding: 5px;
                            font-size: 23px;
                        }

                        .agndatable th {
                            text-decoration: underline;
                        }
                     
                        table td {
                            padding: 10px;
                            font-family: Nyala;
                            font-size: 16px;
                            width: auto;
                        }
      .textbox-pair {
    display: flex;
    align-items: center; /* Vertically aligns the span and input */
    gap: 5px; /* Adds space between the span and input */
    font-size: 16px;
   width:130px;
}

                        /* Additional styles */
                        .nyala-bold {
                            font-family: Nyala;
                            font-weight: bold;
                            font-size: 14px;
                            color: #80ccff;
                        }
                         .nyala-bold1 {
                            font-family: Nyala;
                            font-size: 20px;
                            width: auto;
                            margin-left: 120px;
                        }
                        .nyala-bold0 {
                            font-family: Nyala;
                            font-size: 16px;
                            width: auto;
                            margin-left: 120px;
                        }

                        .custom-hr {
                            border: 1;
                            height: 6px;
                            background-color: #0000b3;
                            width: 100%;
                            margin: 20px 0;
                        }

                        .tablecbordform {
                            width: 45%;
                            border: 1px solid black;
                            padding: 2px;
                            text-align: left;
                            height: 10px;
                        }

                        .pargsig {
                            font-family: Nyala;
                            font-size: 29px;
                        }

                        .pargsig1 {
                            font-family: Nyala;
                            font-size: 32px;
                            text-decoration: underline;
                        }

                        /* Ensure correct layout with no resizing */
                        .container {
                            display: flex;
                            gap: 20px;
                            width: 100%;
                        }

                        .column {
                            display: flex;
                            flex-direction: column;
                            gap: 10px;
                            width: 250px;
                        }

                        .inline-container {
                            display: flex;
                            align-items: center;
                            gap: 5px;
                            background-color: white;
                            height: 30px;
                        }

                        .inline-container span {
                            font-size: 16px;
                            font-weight: bold;
                        }

                        /* Adjust input styles for printing */
                        input[type="text"] {
                            width: 100%; /* Ensure textboxes take full width */
                            padding: 8px;
                            border: 1px solid #ccc;
                            border-radius: 5px;
                            font-size: 16px;
                        }
                    }
                </style>
            </head>
            <body onload="window.print(); window.close();">
                <div class="print-container">
                    ${printContents}
                </div>
            </body>
            </html>
        `);

        // Close the document stream
        printWindow.document.close();
    } catch (error) {
        console.error("An error occurred while trying to print:", error);
        alert("An error occurred while trying to print. Please check the console for details.");
    }
}
