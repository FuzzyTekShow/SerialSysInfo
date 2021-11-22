#include <Wire.h>
#include <Adafruit_GFX.h>
#include <Adafruit_SSD1306.h>
Adafruit_SSD1306 display(128, 64, &Wire, -1);

const byte dataLength = 100;
char receivedData[dataLength];
boolean newData = false;
unsigned long currentMillis;
boolean wasInSerialMode = false;


/*
 * SETUP
 */
void setup() {
  // Enable serial
  Serial.begin(9600);
  // Init display
  display.begin(SSD1306_SWITCHCAPVCC, 0x3C);
  //display.setRotation(2); // Flip 180
  display.setTextSize(2);
  display.setTextColor(SSD1306_WHITE);
  display.clearDisplay();
  display.display();
}


// Enable a reset function
void(* resetFunc) (void) = 0;


/*
 * Takes the serial data and converts it nicely for the display
 */
void SortSerialData(){
    if (newData == true) {
      DisplaySerialData();
      newData = false;
    }
}


/*
 * Displays the data on the display
 */
void DisplaySerialData(){
  // Set the data from the received
  // Example data:
  // 56,75|43|3680,018|1252,573|3610,272|8,333333|11,36697|6,777683|15,92783|42,55247|16:58|Mon27|0
  // 0 = CPU Temp
  // 1 = GPU Temp
  // 2 = CPU Freq
  // 3 = GPU Core
  // 4 = GPU Mem Freq
  // 5 = CPU Load Percentage
  // 6 = GPU Load Percentage
  // 7 = Used RAM
  // 8 = Total RAM
  // 9 = RAM Percentage
  // 10 = Time (00:00)
  // 11 = Day & Date (DAY00)
  // 12 = Screen blanking (1 = blank, 0 = normal)
  
  // Split up the serial data received
  char *cpuT = strtok(receivedData, "|");
  char *gpuT = strtok(NULL, "|");
  char *cpuF = strtok(NULL, "|");
  char *gpuC = strtok(NULL, "|");
  char *gpuM = strtok(NULL, "|");
  char *cpuL = strtok(NULL, "|");
  char *gpuL = strtok(NULL, "|");
  char *usedR = strtok(NULL, "|");
  char *totalR = strtok(NULL, "|");
  char *percentageR = strtok(NULL, "|");
  char *currentTime = strtok(NULL, "|");
  char *currentDate = strtok(NULL, "|");
  char *blank = strtok(NULL, "|");

  // Set the needed floats
  float cpuTemp = strtod(cpuT, NULL);
  float gpuTemp = strtod(gpuT, NULL);
  float cpuLoad = strtod(cpuL, NULL);
  float gpuLoad = strtod(gpuL, NULL);
  float usedRAM = strtod(usedR, NULL);
  float totalRAM = strtod(totalR, NULL);
  float percentageRAM = strtod(percentageR, NULL);
  int blankScreen = strtod(blank, NULL);

  // If the screen is set to blank
  if (blankScreen == 1){
    display.clearDisplay();
  }  else{
    // Move the cursor to the start of the display
    display.setCursor(0, 0);
    
    // Set big font for the time
    display.setTextSize(2);
    
    // Display the time
    display.print(currentTime);
    
    // Set font 1x
    display.setTextSize(1);
    
    // Date
    display.print(" "); // Blank space
    display.println(currentDate);
    
    // Create an underline for the date
    display.println("           ----------");
  
    // Text to large size
    display.setTextSize(2); 
    
    // CPU Information
    display.print("CPU: ");
    display.print(cpuTemp, 1);
    display.println("c");
    // GPU Information
    display.print("GPU: ");
    display.print(gpuTemp, 1);
    display.println("c");
    
    // Text to small size
    //display.setTextSize(1);
    
    /*
    // Frequency information
    display.print("CPU:");
    display.print(gpuM);
    display.print("GPU:");
    display.println(cpuF);
    */
    
    // RAM Information
    display.print("RAM: ");
    //display.print(usedRAM, 1);
    //display.print("GB ");
    /*
    display.print(totalRAM, 1);
    display.println("GB");
    */
    //display.print("(");
    display.print(percentageRAM, 0);
    display.println("%");
    
    
    display.display();
  }

}


/*
 * Reads the serial data
 */
void ReadSerial() {
    static byte index = 0;
    char endMarker = '\n';
    char c;
    unsigned long startMillis;
    currentMillis = millis();

    while (Serial.available() > 0 && newData == false) {
      c = Serial.read();
      
      display.clearDisplay();

      
      if (c != endMarker) {
          receivedData[index] = c;
          index++;
          if (index >= dataLength) {
              index = dataLength - 1;
          }
      }
      else {
          receivedData[index] = '\0'; // terminate the string
          index = 0;
          newData = true;
      }

      startMillis = millis();
      currentMillis = millis();
      wasInSerialMode = true;
    }


   // If the serial hasn't been available for more than 10 seconds
   if (currentMillis - startMillis > 10000){
    display.clearDisplay();
    display.display();
   }
    /*
      // 10 minutes
      if (currentMillis - startMillis > 600000){
        display.clearDisplay();  
      }else{
        if (wasInSerialMode){
          display.clearDisplay();
          wasInSerialMode = false;
        }
        display.setTextSize(2);
        display.setCursor(0,0);
        // Display the panda
        display.drawBitmap(0,0,panda,128,32,1);
        display.display();
      }
    }
    */
}


/*
 * MAIN LOOP
 */
void loop() {
  ReadSerial();
  SortSerialData();
}
