//******************************************************
// LED Panel and Presentation OUTPUT
const int LED1 = 13;
const int LED2 = 12;
const int LED3 = 11;
const int LED4 = 10;
const int LED5 = 9;
const int LED6 = 8;
const int LED7 = 7;
const int LED8 = 6;
const int LED9 = 5;
const int LED_P = 4;

// Buttons (D. INPUT)
const int BN1 = 3;
const int BN2 = 2;
// Buttons (A. INPUT) - Analog pins are INPUT by default
#define BN3 A2
#define BN4 A3
#define BN5 A4

// Diode Inputs
#define D1 A0
#define D2 A1
//******************************************************
// Variables to keep track
/* Order of variables - 1:ALL ON - 2:AUTO OFF - 3:AUTO - 4:Custom
 Presentation is separate.
 states are switched by using different buttons. presentation is turned on and off 
by toggling same button. its also connected to other states.*/
int state_of4 = 2; // All off initially
byte presentationState = 0; // Presentation off initially
bool auto_transition = false; // transition to auto mode
int previous_state = 2;

//******************************************************
// Serial Comm variables and functions-------------------------------
String COMstring; // String to store input from Serial Port
String COMsequence; // String to store input sequence from Serial Port
int sequence[9] = {1, 0, 1,
                   0, 1, 0,
                   1, 0, 1};

#define STRING2SEQUENCE for(int i=0; i<9; i++) {sequence[i] = (COMsequence.charAt(i)=='1') ? 1 : 0;}
#define SWITCHBYSEQUENCE(seq, num) for(int i=0; i<num; i++) {(seq[i]==1) ? digitalWrite(getLedPin(i), HIGH) : digitalWrite(getLedPin(i), LOW);}

// Sequences----------------------------------------------------------
// All LEDs and presentation on seqeunce
int all_on[10] = {1, 1, 1,
                  1, 1, 1,
                  1, 1, 1,
                  1};
// All LEDs and presentation off seqeunce
int all_off[10] = {0,};
// Automatic Diode input sequences (2-inputs -> 9-outputs)
int auto_1[9] = {0, 0, 0,        /* D1=1 D2=1*/
                 0, 0, 0,
                 1, 1, 1};
int auto_2[9] = {0, 0, 1,        /* D1=1 D2=0*/
                 0, 0, 1,
                 1, 1, 1};
int auto_3[9] = {1, 0, 0,        /* D1=0 D2=1*/
                 1, 0, 0,
                 1, 1, 1};
#define auto_4 all_on            // D1=0 D2=0

const int getLedPin(int index) // For turning on LEDs based on sequence
{
  switch(index)
  {
    case 0:
    return LED1;
    case 1:
    return LED2;
    case 2:
    return LED3;
    case 3:
    return LED4;
    case 4:
    return LED5;
    case 5:
    return LED6;
    case 6:
    return LED7;
    case 7:
    return LED8;
    case 8:
    return LED9;
    case 9:
    return LED_P;
    default:
    return -1; // error
  }
}

// Function to toggle LED presentation with software debouncing
void togglePresentation() {
  static unsigned long lastDebounceTime = 0;
  unsigned long debounceDelay = 1000; // Adjust debounce delay as needed
  unsigned long currentTime = millis();

  if (currentTime - lastDebounceTime >= debounceDelay) {
    // Toggle presentation state
    presentationState = !presentationState;
    digitalWrite(LED_P, presentationState);
    (presentationState) ? Serial.println("*P1\r\n") : Serial.println("*P0\r\n");
    lastDebounceTime = currentTime;
  }
}
//******************************************************

// Setup Pins
#define PIN_OUT(num) pinMode(LED##num, OUTPUT);
#define PIN_IN(num) pinMode(BN##num, INPUT);

//******************************************************
void setup()
{
  PIN_OUT(1) PIN_OUT(2) PIN_OUT(3)
  PIN_OUT(4) PIN_OUT(5) PIN_OUT(6)
  PIN_OUT(7) PIN_OUT(8) PIN_OUT(9)
  pinMode(LED_P, OUTPUT);
  PIN_IN(1) PIN_IN(2)
  // Start Serial Communication
  Serial.begin(9600);
}
void loop()
{
  // Get Serial Port Input from Desktop CLI------------------------------------
  if(Serial.available())
  {
    COMstring = Serial.readString();
    if(COMstring.charAt(0)=='#' && COMstring.charAt(1)=='C')
    {
      // to get only sequence from '#C111000111\r\n' format and convert to int array
      COMsequence = COMstring.substring(2,11);
      STRING2SEQUENCE
      state_of4 = 4; // Set state to Custom mode
      Serial.println("*M4\r\n");
    }
    if(COMstring.charAt(0)=='#' && COMstring.charAt(1)=='S')
    {
       Serial.println("*M2\r\n");
       delay(2000);
       Serial.println("*P0\r\n");
    }
  }
  // Read button and sensor inputs and assign states
  if(digitalRead(BN1))            // 1: Turn all ON
  {
    state_of4 = 1;
  }
  else if(digitalRead(BN2))       // 2: Turn all OFF
  {
    state_of4 = 2;
  }
  else if(digitalRead(BN3))       // 3: AUTO mode
  {
    state_of4 = 3;
    auto_transition = true;
  }
  else if(analogRead(BN5))        // 4: Custom mode
  {
    state_of4 = 4;
  }
  if(analogRead(BN4))            // Toggle Presentation---------
  {
    /*presentationState = !presentationState;
    digitalWrite(LED_P, presentationState);
    (presentationState) ? Serial.println("#P1\r\n") : Serial.println("#P0\r\n");*/
    togglePresentation();
    delay(200);
  }

  // Do processes as per states
  switch (state_of4)
  {
    case 1:
    if(previous_state == 1) break;
    SWITCHBYSEQUENCE(all_on, 10);
    presentationState = 1;
    Serial.println("*M1\r\n");
    state_of4 = 0; // Reset state
    previous_state = 1;
    break;
    case 2:
    if(previous_state == 2) break;
    SWITCHBYSEQUENCE(all_off, 10);
    presentationState = 0;
    Serial.println("*M2\r\n");
    state_of4 = 0;
    previous_state = 2;
    break;
    case 3:
    if(digitalRead(D1) && digitalRead(D2))
    {
      SWITCHBYSEQUENCE(auto_1, 9);
    }
    else if(digitalRead(D1) && !digitalRead(D2))
    {
      SWITCHBYSEQUENCE(auto_2, 9);
    }
    else if(!digitalRead(D1) && digitalRead(D2))
    {
      SWITCHBYSEQUENCE(auto_3, 9);
    }
    else
    {
      SWITCHBYSEQUENCE(auto_4, 9);
    }
    state_of4 = 3; // Keep same state
    if(auto_transition==true && previous_state != 3)
    {
      Serial.println("*M3\r\n");
    }
    auto_transition = false; // transition over
    previous_state = 3;
    break;
    case 4:
    for(int i=0; i<9; i++)
    {
      (sequence[i]==1) ? digitalWrite(getLedPin(i), HIGH) : digitalWrite(getLedPin(i), LOW);
    }
    if(previous_state != 4)
    {
      Serial.println("*M4\r\n");
    }
    state_of4 = 0;
    previous_state = 4;
    break;
    default:
    break;
  }
}
//******************************************************
