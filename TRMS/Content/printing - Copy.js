
    function printDiv() {
        var printContents = document.getElementById("printArea").innerHTML;
    var originalContents = document.body.innerHTML;
   
    var printWindow = window.open('', '_blank');
    printWindow.document.write(`
            <html>

        <head>
            <title>Print</title>
            <style>
   @media print {
.custom-logo {
    width: 102px;
    height: 80px;
}
    .agndatable {
        width: 1500px;
        border-collapse: collapse;
    }
    .agndatable th, .agndatable td {
        border: 1px solid black;
        padding: 5px;
       font-size:23px;
    }
.agndatable th
   {
  text-decoration: underline;
   }
 .print-container {
   width:2110px;
            }
        }
.barcodim{
  height: 100px;
width: auto;
}
.nyala-bold {
    font-family: Nyala;
    font-weight: bold;
    font-size: 14px;
    width: auto;
    margin-left: 200px;
    color: #80ccff;
}
.nyala-bold0 {
    font-family: Nyala;
    font-weight: bold;
    font-size: 30px;
    width: auto;
    margin-left: 121px;
    padding-top: 1021px;
    color: #80ccff;


}

.nyala-bold1 {
    font-family: Nyala;
    font-size: 30px;
    width: auto;
    margin-left: 120px;
}

.custom-hr {
    border: 1;
    height: 6px;
    background-color:#0000b3; /* Green */
    width: 100%;
    margin: 20px 0;
}
.custom-hr1 {
    border: 1;
    height: 2px;
    background-color:#0000b3; /* Green */
    width: 100%;
    margin: 20px 0;
}


table td {
    padding: 10px; /* Reduce padding to 5px or any desired value */
    font-family: Nyala;
    font-size: 30px;
    width: auto;
    margin-left: 130px;
}

.voteform th {
    text-decoration: underline;
    font-family: Nyala;
    font-weight: bold;
    font-size: 30px;
    width: auto;
}

.voteform1 td {
    font-family: Nyala;
    font-weight: bold;
    font-size: 30px;
    width: auto;
    padding-left:13px;
    padding-top: 32px;

}

.rounded-input {
    border: 1px solid #ccc; /* Border color */
    border-radius: 8px; /* Rounded corners */
    width: 100px; /* Width of the textbox */
    height: 40px; /* height of the textbox */
    font-size: 16px; /* Font size */
}

    .rounded-input:focus {
        border-color: #4CAF50; /* Change border color on focus */
    }

.agenda {
    text-decoration: underline;
}

.agenda1 td {
    padding: 5px; /* Reduce padding to 5px or any desired value */
    font-family: Nyala;
    font-size: 30px;
    width: auto;
}

.tablecbordform {
                    width: 45%;
                    border: 1px solid black;
                    padding: 2px;
                    text-align: left;
                   height: 10px;
                }

               

                .texboxs {
                    width: 400px;
                    height: 20px;
                }


.agndatable td {
    border: 1px solid black;
}
.pargsig {
    font-family: Nyala;
    font-size: 29px;
}
.pargsig1 {
font-family: Nyala; 
font-size:32px;
text-decoration: underline;
}
.agendadiv {
   margin-left:150px;
margin-top:1050px;
}
 .textbox-container1 {
                    display: flex;
                    flex-direction: column;
                    gap: 2px;
                    width:100%
                    margin: 0 400px;
                }
               .textbox-container1 input[type="text"] {
                        border: 1px solid #ccc;
                        border-radius: 10px;
                        padding: 8px 12px;
                        font-size: 14px;
                        width: 100%;
                        box-sizing: border-box;
                    }
                .container {
                    display: flex;
                    gap: 20px; /* Space between the two columns */
                }

                .column {
                    display: flex;
                    flex-direction: column; /* Items stack vertically */
                    gap: 10px; /* Space between items in the column */
                    width:250px;
                }

                .inline-container {
                    display: flex;
                    align-items: center; /* Align number and text vertically */
                    gap: 5px; /* Space between number and text */
                    background-color:white;
                    height:30px;
                }

                    .inline-container span {
                        font-size: 16px;
                        font-weight: bold;
                    }

                    .inline-container h4 {
                        margin: 0; /* Remove default margin */
                        font-size: 16px;
                    }
                .textbox-container2 {
                    margin-top: -250px;
                    margin-left: 700px;
                    display: flex;
                    flex-direction: column; /* Ensures all items stack vertically */
                    gap: 10px; /* Adds space between items */
                    width:120px;
                }

                .textbox-pair {
                    display: flex;
                    align-items: center; /* Vertically aligns the span and input */
                    gap: 5px; /* Adds space between the span and input */
                }

                input[type="text"] {
                    width: 10px; /* Set a consistent width for all textboxes */
                    padding: 8px;
                    border: 1px solid #ccc;
                    border-radius: 5px;
                    font-size: 100px;
                }
                </style>
        </head>
        <body onload="window.print(); window.close();">
            <div class="print-container">
         
     ${printContents}</div>
        </body>
    </html>
    `);
    printWindow.document.close();
}
