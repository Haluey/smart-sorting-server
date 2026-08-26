# Smart Sorting 시스템 분류 결과 및 오류 처리 기준

## 1. 제품 분류 결과

| No. | 상황 | classificationStatus | productTypeCode | confidence | imagePath |
|:---:|:---|:---:|:---:|:---:|:---|
| 1 | CANDY 정상 분류 | `SUCCESS` | `CANDY` | `0.77` | `/home/rpi/test/photos/photo_001.jpg` |
| 2 | CHOCOLATE 정상 분류 | `SUCCESS` | `CHOCOLATE` | `0.91` | `/home/rpi/test/photos/photo_002.jpg` |
| 3 | CANDY Confidence 부족 | `FAILED` | `null` | `0.63` | `/home/rpi/test/photos/photo_003.jpg` |
| 4 | CHOCOLATE Confidence 부족 | `FAILED` | `null` | `0.58` | `/home/rpi/test/photos/photo_004.jpg` |
| 5 | YOLO 모델 로딩 오류 | `FAILED` | `null` | `0.00` | `null` |
| 6 | YOLO Inference 오류 | `FAILED` | `null` | `0.00` | `/home/rpi/test/photos/photo_006.jpg` |
| 7 | YOLO 객체 미검출 | `FAILED` | `null` | `0.00` | `/home/rpi/test/photos/photo_005.jpg` |
| 8 | Camera 촬영 오류 | `FAILED` | `null` | `0.00` | `null` |
| 9 | 이미지 저장 오류 | `FAILED` | `null` | `0.00` | `null` |
| 10 | CANDY + Servo ACK Timeout | `SUCCESS` | `CANDY` | `0.84` | `/home/rpi/test/photos/photo_009.jpg` |
| 11 | CHOCOLATE + Servo ACK Timeout | `SUCCESS` | `CHOCOLATE` | `0.88` | `/home/rpi/test/photos/photo_010.jpg` |
| 12 | CANDY + 잘못된 Servo ACK | `SUCCESS` | `CANDY` | `0.82` | `/home/rpi/test/photos/photo_011.jpg` |
| 13 | CHOCOLATE + 잘못된 Servo ACK | `SUCCESS` | `CHOCOLATE` | `0.95` | `/home/rpi/test/photos/photo_012.jpg` |
| 14 | Arduino Serial 연결 끊김 | `FAILED` | `null` | `0.00` | `null` |
| 15 | Arduino Serial 통신 오류 | `FAILED` | `null` | `0.00` | `null` |
| 16 | Serial 응답 Timeout | `FAILED` | `null` | `0.00` | `/home/rpi/test/photos/photo_015.jpg` |
| 17 | IR Sensor 오류 | `FAILED` | `null` | `0.00` | `null` |
| 18 | Stepper Motor 오류 | `FAILED` | `null` | `0.00` | `null` |


## 2. Component 상태 및 오류

| No. | 상황 | componentCode | status | errorCode | message | priority |
|:---:|:---|:---:|:---:|:---:|:---|:---:|
| 1 | IR Sensor 정상 | `IR_SENSOR` | `NORMAL` | `null` | `IR 센서가 정상적으로 동작하고 있습니다.` | `null` |
| 2 | Camera 정상 | `CAMERA` | `NORMAL` | `null` | `카메라가 정상적으로 동작하고 있습니다.` | `null` |
| 3 | Conveyor 정상 | `CONVEYOR` | `NORMAL` | `null` | `컨베이어가 정상적으로 동작하고 있습니다.` | `null` |
| 4 | Servo 정상 | `SORTING_SERVO` | `NORMAL` | `null` | `분류 Servo의 동작이 정상적으로 확인되었습니다.` | `null` |
| 5 | Arduino 정상 | `ARDUINO` | `NORMAL` | `null` | `Arduino 및 Serial 통신이 정상적으로 동작하고 있습니다.` | `null` |
| 6 | Worker Display 정상 | `WORKER_DISPLAY` | `NORMAL` | `null` | `작업자용 Display가 정상적으로 동작하고 있습니다.` | `null` |
| 7 | Buzzer 정상 | `BUZZER` | `NORMAL` | `null` | `부저가 정상적으로 동작하고 있습니다.` | `null` |
| 9 | IR Sensor 오류 | `IR_SENSOR` | `ERROR` | `IR_ERROR` | `IR 센서가 정상적으로 동작하지 않습니다.` | `HIGH` |
| 10 | Camera 촬영 오류 | `CAMERA` | `ERROR` | `CAMERA_ERROR` | `카메라 촬영에 실패했습니다.` | `HIGH` |
| 11 | YOLO Inference 오류 | `CAMERA` | `ERROR` | `YOLO_ERROR` | `YOLO 추론에 실패하여 제품을 분류할 수 없습니다.` | `HIGH` |
| 12 | YOLO 모델 로딩 오류 | `CAMERA` | `ERROR` | `MODEL_LOAD_ERROR` | `YOLO 모델을 불러오지 못했습니다.` | `HIGH` |
| 13 | YOLO 객체 미검출 | `CAMERA` | `WARNING` | `NO_DETECTION` | `YOLO에서 제품 객체를 검출하지 못했습니다.` | `MEDIUM` |
| 14 | 이미지 저장 오류 | `CAMERA` | `ERROR` | `IMAGE_SAVE_ERROR` | `촬영한 이미지 저장에 실패했습니다.` | `HIGH` |
| 15 | Servo ACK Timeout | `SORTING_SERVO` | `ERROR` | `SERVO_ACK_TIMEOUT` | `예상한 Servo ACK 응답을 제한 시간 내에 받지 못했습니다.` | `HIGH` |
| 16 | Servo ACK 오류 | `SORTING_SERVO` | `ERROR` | `SERVO_ACK_ERROR` | `예상한 Servo ACK와 다른 응답을 수신했습니다.` | `HIGH` |
| 17 | Stepper Motor 오류 | `CONVEYOR` | `ERROR` | `STEPPER_ERROR` | `Conveyor Stepper Motor가 정상적으로 동작하지 않습니다.` | `HIGH` |
| 18 | Buzzer 오류 | `BUZZER` | `ERROR` | `BUZZER_ERROR` | `Buzzer가 정상적으로 동작하지 않습니다.` | `HIGH` |
| 19 | Arduino 연결 끊김 | `ARDUINO` | `OFFLINE` | `SERIAL_DISCONNECTED` | `Arduino와의 Serial 연결이 끊어졌습니다.` | `HIGH` |
| 20 | Arduino Serial 통신 오류 | `ARDUINO` | `ERROR` | `SERIAL_ERROR` | `Arduino Serial 통신 중 오류가 발생했습니다.` | `HIGH` |
| 21 | Arduino Serial Timeout | `ARDUINO` | `ERROR` | `SERIAL_TIMEOUT` | `Arduino의 Serial 응답을 제한 시간 내에 받지 못했습니다.` | `HIGH` |
| 22 | Arduino 동작 오류 | `ARDUINO` | `ERROR` | `ARDUINO_ERROR` | `Arduino가 정상적으로 동작하지 않습니다.` | `HIGH` |
| 23 | 알 수 없는 Arduino 명령 | `ARDUINO` | `ERROR` | `UNKNOWN_COMMAND` | `Arduino에서 알 수 없는 명령을 수신했습니다.` | `HIGH` |


## 3. 처리 기준

### 3.1 제품 분류 기본 기준

| 조건 | 제품 분류 결과 |
|:---|:---|
| Confidence > 0.70 | `SUCCESS` + 제품 종류 확정 |
| Confidence <= 0.70 | `FAILED` + `productTypeCode = null` |
| Class 0 | `CANDY` |
| Class 1 | `CHOCOLATE` |

### 3.2 제품 분류 결과 + Component 상태 처리

| 상황 | 제품 분류 결과 | Component 상태 처리 |
|:---|:---|:---|
| YOLO 객체 미검출 | `FAILED` + `productTypeCode = null` | `CAMERA = WARNING` + `NO_DETECTION` |
| YOLO Inference 오류 | `FAILED` + `productTypeCode = null` | `CAMERA = ERROR` + `YOLO_ERROR` |
| YOLO 모델 로딩 오류 | `FAILED` + `productTypeCode = null` | `CAMERA = ERROR` + `MODEL_LOAD_ERROR` |
| Camera 촬영 실패 | `FAILED` + `productTypeCode = null` | `CAMERA = ERROR` + `CAMERA_ERROR` |
| 이미지 저장 실패 | `FAILED` + `productTypeCode = null` | `CAMERA = ERROR` + `IMAGE_SAVE_ERROR` |
| Arduino Serial 연결 끊김 | `FAILED` + `productTypeCode = null` | `ARDUINO = OFFLINE` + `SERIAL_DISCONNECTED` |
| Arduino Serial 통신 오류 | `FAILED` + `productTypeCode = null` | `ARDUINO = ERROR` + `SERIAL_ERROR` |
| Serial 응답 Timeout | `FAILED` + `productTypeCode = null` | `ARDUINO = ERROR` + `SERIAL_TIMEOUT` |
| IR Sensor 오류 | `FAILED` + `productTypeCode = null` | `IR_SENSOR = ERROR` + `IR_ERROR` |
| Stepper Motor 오류 | `FAILED` + `productTypeCode = null` | `CONVEYOR = ERROR` + `STEPPER_ERROR` |
| Servo ACK Timeout | 제품 분류 결과 유지 | `SORTING_SERVO = ERROR` + `SERVO_ACK_TIMEOUT` |
| Servo ACK 불일치 | 제품 분류 결과 유지 | `SORTING_SERVO = ERROR` + `SERVO_ACK_ERROR` |

### 3.3 Component 상태만 처리하는 경우

| 상황 | Component 상태 처리 |
|:---|:---|
| Servo ACK 정상 | `SORTING_SERVO = NORMAL` |
| Arduino 동작 오류 | `ARDUINO = ERROR` + `ARDUINO_ERROR` |
| 알 수 없는 Arduino 명령 | `ARDUINO = ERROR` + `UNKNOWN_COMMAND` |
| Buzzer 오류 | `BUZZER = ERROR` + `BUZZER_ERROR` |

### 3.4 정상 상태 처리

시스템 시작 또는 장비 상태 복구 시 제어부에서 `NORMAL` 상태를 전달한다.

```text
component/status/update 수신
        ↓
status = NORMAL
        ↓
system_components.current_status = NORMAL
        ↓
Alert 생성하지 않음
```

- 정상 상태에서는 `errorCode = null`
- 정상 상태에서는 `priority = null`
- `NORMAL` 메시지는 현재 Component 상태 갱신 목적으로 사용한다.

### 3.5 기타 제어 흐름

| 조건 | 처리 결과 |
|:---|:---|
| PROCESS_DONE 수신 | Arduino Conveyor 재시작 |


## 4. Error Code

### Hardware Error Code

| Error Code | 설명 | Component |
|:---|:---|:---:|
| `CAMERA_ERROR` | 카메라 촬영 또는 카메라 동작 오류 | `CAMERA` |
| `SERVO_ACK_TIMEOUT` | Servo ACK 응답 시간 초과 | `SORTING_SERVO` |
| `SERVO_ACK_ERROR` | 예상하지 않은 Servo ACK 수신 | `SORTING_SERVO` |
| `STEPPER_ERROR` | Stepper Motor 오류 | `CONVEYOR` |
| `IR_ERROR` | IR Sensor 오류 | `IR_SENSOR` |
| `BUZZER_ERROR` | Buzzer 동작 오류 | `BUZZER` |
| `ARDUINO_ERROR` | Arduino 동작 오류 | `ARDUINO` |

### Software Error Code

| Error Code | 설명 | Component |
|:---|:---|:---:|
| `YOLO_ERROR` | YOLO Inference 오류 | `CAMERA` |
| `MODEL_LOAD_ERROR` | YOLO 모델 로딩 오류 | `CAMERA` |
| `NO_DETECTION` | YOLO 객체 미검출 | `CAMERA` |
| `IMAGE_SAVE_ERROR` | 이미지 저장 오류 | `CAMERA` |
| `SERIAL_DISCONNECTED` | Arduino Serial 연결 끊김 | `ARDUINO` |
| `SERIAL_ERROR` | Serial 통신 오류 | `ARDUINO` |
| `SERIAL_TIMEOUT` | Serial 응답 시간 초과 | `ARDUINO` |
| `UNKNOWN_COMMAND` | 알 수 없는 Arduino 명령 | `ARDUINO` |


## 5. 실제 DB 구조

### Component Code

| componentCode | 대상 | Type |
|:---:|:---|:---:|
| `RASPBERRY_PI` | 라즈베리파이 5 | `CONTROLLER` |
| `ARDUINO` | 아두이노 제어 보드 | `CONTROLLER` |
| `IR_SENSOR` | 제품 투입 감지 센서 | `SENSOR` |
| `CAMERA` | 제품 분류 카메라 및 YOLO | `SENSOR` |
| `CONVEYOR` | 컨베이어 벨트 | `ACTUATOR` |
| `SORTING_SERVO` | 제품 분류 서보모터 | `ACTUATOR` |
| `BUZZER` | 알림 부저 | `ACTUATOR` |
| `WORKER_DISPLAY` | 작업자 LCD 장비 | `DISPLAY` |
| `WORKER_UI` | 작업자 화면 프로그램 | `SOFTWARE` |
| `ADMIN_WEB` | 관리자 웹 프로그램 | `SOFTWARE` |
| `MQTT_BROKER` | MQTT 브로커 | `SOFTWARE` |
| `API_SERVER` | ASP.NET Core API 서버 | `SERVER` |
| `MYSQL_DATABASE` | MySQL 데이터베이스 | `DATABASE` |

### Component Status

| status | 의미 |
|:---:|:---|
| `NORMAL` | 정상 동작 |
| `WARNING` | 경고 |
| `ERROR` | 오류 발생 |
| `OFFLINE` | 연결 불가 또는 장치 오프라인 |

### Alert Priority

| priority | 의미 |
|:---:|:---|
| `LOW` | 정상 처리 결과나 단순 안내 등 낮은 중요도의 알림 |
| `MEDIUM` | 생산에 영향을 줄 수 있어 확인이 필요한 알림 |
| `HIGH` | 장비 정지, 연결 끊김 등 즉시 확인 및 조치가 필요한 알림 |


## 6. MQTT Topic

| Topic | 용도 |
|:---|:---|
| `smart_sorting/camera/product_detection` | YOLO 제품 분류 결과 |
| `smart_sorting/component/status/update` | 컴포넌트 상태 및 오류 |

### `smart_sorting/camera/product_detection`

제품 분류 결과 전달

```json
{
  "classificationStatus": "SUCCESS",
  "productTypeCode": "CHOCOLATE",
  "confidence": 0.91,
  "imagePath": "/home/rpi/test/photos/photo_002.jpg"
}
```

- `classificationStatus`
    - `SUCCESS`
    - `FAILED`

- `productTypeCode`
    - `CHOCOLATE`
    - `CANDY`
    - 분류 실패 시 `null`

- `confidence`
    - `0.00 ~ 1.00`

- `imagePath`
    - 저장된 이미지 경로
    - 이미지가 없는 경우 `null`

### `smart_sorting/component/status/update`

Component 상태 및 오류 전달

```json
{
  "componentCode": "CAMERA",
  "status": "ERROR",
  "errorCode": "CAMERA_ERROR",
  "message": "카메라 촬영에 실패했습니다."
}
```

- `componentCode`
    - **Component Code** 중 하나

- `status`
    - `NORMAL`
    - `WARNING`
    - `ERROR`
    - `OFFLINE`

- `errorCode`
    - **Component 상태 및 오류**에 정의된 Error Code 중 하나
    - 정상 상태는 `null`

- `message`
    - **Component 상태 및 오류**에 정의된 메시지
