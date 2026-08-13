# 개발 및 설계 진행 기록

스마트 분류 시스템 서버와 데이터베이스를 설계하면서 검토한 내용, 변경 이유, 완료 작업을 날짜별로 정리한 문서입니다.

---

## 2026-07-31 — 초기 데이터베이스 구조 정리

### 진행 내용

- 생산 작업을 중심으로 사용자, 제품, 제품 유형, 시스템 구성요소, 알림 구조를 정리했습니다.
- 초기 ERD에 다음 6개 테이블을 구성했습니다.
  - `USERS`
  - `PRODUCTION_SESSIONS`
  - `PRODUCTS`
  - `PRODUCT_TYPES`
  - `SYSTEM_COMPONENTS`
  - `ALERTS`
- 모든 테이블 관계를 비식별 관계로 설계했습니다.
- 작업자별 생산량과 제품별 분류 결과, 장비 오류 이력을 연결할 수 있도록 관계를 정의했습니다.

### 초기 설계에서 확인된 문제

- `PRODUCTS`라는 이름이 제품 기준 정보 테이블처럼 보일 수 있었습니다.
- `production_date`와 `started_at`은 날짜 정보가 중복될 수 있었습니다.
- `chocolate_set_count`는 `chocolate_count`에서 계산할 수 있었습니다.
- `PRODUCT_TYPES`의 `unit_type`, `set_quantity`, `is_active`는 현재 고정 제품 구조에 비해 복잡했습니다.
- `ALERTS`의 `result_status`, `status`, `severity`는 역할이 바로 드러나지 않았습니다.
- 알림에 발생 사용자와 확인 사용자를 함께 저장해 의미가 겹칠 수 있었습니다.
- 시스템 구성요소를 이름으로만 조회하면 프로그램 연동 시 이름 변경의 영향을 받을 수 있었습니다.

---

## 2026-08-03 — 데이터베이스 구조 초안 수정

### 진행 내용

- 기존 ERD를 기준으로 실제 구현에 필요한 테이블과 컬럼을 다시 검토했습니다.
- `USERS`, `PRODUCT_TYPES`, `SYSTEM_COMPONENTS` 테이블을 SQL로 옮기기 위한 초안을 작성했습니다.
- 제품 유형과 시스템 구성요소에 프로그램 연동용 고정 식별값을 추가하기로 했습니다.
- 일반적인 `code` 대신 역할이 드러나는 컬럼명으로 정리했습니다.
  - `product_type_code`
  - `component_code`
- 시스템 구성요소의 상태 컬럼을 `status`에서 `current_status`로 변경했습니다.
- Raspberry Pi와 Arduino를 표현하기 위해 구성요소 유형에 `CONTROLLER`를 추가했습니다.
- 시스템 구성요소 상태를 `NORMAL`, `WARNING`, `ERROR`, `OFFLINE`으로 정리했습니다.
- 시스템 실행 전 초기 상태는 `OFFLINE`으로 등록하기로 했습니다.

### 검토 및 결정 사항

- 시스템 구성요소 테이블에는 현재 상태만 저장하고, 상세 오류 내용과 과거 이력은 `alerts`에서 관리하기로 했습니다.
- 제품 투입 감지용 적외선 센서는 실제 구성에 맞춰 1개만 등록하기로 했습니다.
- 서보모터는 제품 분류 및 배출용 1개로 정리했습니다.
- 작업자 LCD 장비와 작업자 화면 프로그램은 하드웨어와 소프트웨어 상태를 구분하기 위해 각각 관리하기로 했습니다.

---

## 2026-08-04 — SQL 스키마 작성 및 컬럼 구조 개선

### 진행 내용

- MySQL 데이터베이스 생성 스크립트를 작성했습니다.
- 문자 집합을 `utf8mb4`, 정렬 규칙을 `utf8mb4_unicode_ci`로 설정했습니다.
- 모든 기본 키와 외래키 타입을 `BIGINT`로 통일했습니다.
- MySQL `ENUM` 대신 `VARCHAR`와 `CHECK` 제약조건을 사용하도록 변경했습니다.

### 사용자 테이블

- `password`를 `password_hash`로 변경했습니다.
- `login_id`, `name`의 길이를 `VARCHAR(50)`으로 확장했습니다.
- 역할을 `ADMIN`, `WORKER`로 제한했습니다.

### 제품 유형 테이블

- 서버에서 사용할 수 있는 `product_type_code`를 추가했습니다.
- `unit_type`, `set_quantity`를 `unit_per_set`으로 단순화했습니다.
- 현재 초콜릿과 사탕만 고정으로 사용하므로 `is_active`를 제거했습니다.
- 초콜릿 10개, 사탕 1개를 초기 데이터로 정의했습니다.

### 시스템 구성요소 테이블

- 서버와 MQTT에서 이름 대신 안정적으로 조회하도록 `component_code`를 추가했습니다.
- `status`를 `current_status`로 변경했습니다.
- `updated_at`을 `status_updated_at`으로 변경해 의미를 명확히 했습니다.
- 라즈베리파이, 아두이노, 센서, 카메라, 컨베이어, 서보모터, 부저, 화면, 프로그램, 서버, DB를 초기 데이터로 등록했습니다.

### 생산 작업 테이블

- 작업 시작 시 행이 생성되는 구조로 결정했습니다.
- `production_date`를 제거하고 `started_at`에서 날짜를 계산하도록 했습니다.
- `chocolate_set_count`를 제거하고 낱개 수와 제품 유형 기준으로 계산하도록 했습니다.
- 상태를 `RUNNING`, `PAUSED`, `COMPLETED`, `CANCELLED`로 정의했습니다.

---

## 2026-08-05 — 데이터 정합성 검증 및 ERD 완성

### 제품 감지 및 분류 결과

- 테이블명을 `products`에서 `product_detections`로 변경했습니다.
- 기본 키를 `product_id`에서 `product_detection_id`로 변경했습니다.
- 분류 실패를 저장할 수 있도록 `product_type_id`, `confidence`, `image_path`에 `NULL`을 허용했습니다.
- 신뢰도를 `DECIMAL(5,4)`로 변경했습니다.
- 성공 시 제품 유형과 신뢰도가 필요하고, 실패 시 제품 유형이 없어야 한다는 제약조건을 추가했습니다.

### 알림 테이블

- 발생 작업자용 `user_id`를 제거했습니다.
- 알림 발생 당시 작업자는 `alerts → production_sessions → users` 관계로 조회하도록 했습니다.
- 알림 확인 사용자만 `checked_by_user_id`로 직접 저장하도록 역할을 분리했습니다.
- 컬럼명을 다음과 같이 명확하게 변경했습니다.
  - `severity` → `priority`
  - `result_status` → `recovery_status`
  - `status` → `check_status`
  - `message` → `alert_message`
  - `acknowledged_by_user_id` → `checked_by_user_id`
  - `acknowledged_at` → `checked_at`
- 복구 시각을 저장하는 `recovered_at`을 추가했습니다.
- `INFO`는 복구 및 확인 대상이 아니므로 관련 컬럼을 `NULL`로 저장하도록 했습니다.
- 알림 유형과 중요도의 허용 조합을 제약조건으로 정의했습니다.
- 복구 상태와 복구 시각, 확인 상태와 확인 사용자·확인 시각이 일치하도록 검증 조건을 추가했습니다.

### 생산 작업 정합성 검증

- 목표 수량과 생산 수량이 음수가 되지 않도록 했습니다.
- 초콜릿과 사탕의 목표량이 모두 0인 작업 생성을 방지했습니다.
- 진행·일시정지 상태에는 종료 시각이 없어야 하고, 완료·취소 상태에는 종료 시각이 필요하도록 했습니다.

### 초기·더미 데이터

- 제품 유형 2종과 시스템 구성요소 14종의 기준 데이터를 작성했습니다.
- DB 구조 확인을 위한 사용자, 생산 작업, 제품 분류 결과, 알림 더미 데이터를 작성했습니다.
- 더미 사용자 비밀번호는 로그인 구현 후 BCrypt 해시로 교체할 예정입니다.

### ERD 작성

- DBeaver에서 실제 MySQL 외래키를 기반으로 편집용 `.erd` 파일을 작성했습니다.
- ERDCloud에서 논리명, 물리명, PK, FK, NULL 여부와 상태값 메모를 포함한 문서용 ERD를 작성했습니다.
- 테이블 역할에 따라 다음 색상을 적용했습니다.
  - 사용자: 파랑
  - 생산 작업: 초록
  - 제품 감지 결과: 보라
  - 제품 유형: 주황
  - 시스템 구성요소: 청록
  - 알림: 빨강

---

## 2026-08-10 — ASP.NET Core 서버 구축 및 인증·생산 작업 API 구현

### 서버 프로젝트 구축

- .NET 10 기반 ASP.NET Core Web API 프로젝트 생성
- Controller 방식의 API 구조 적용
- OpenAPI 설정 및 프로젝트 기본 실행 확인
- MySQL 연동을 위해 `MySql.EntityFrameworkCore` 적용
- User Secrets를 사용하여 MySQL 연결 정보 분리
- `AppDbContext` 구성 후 기존 `smart_sorting_system` 데이터베이스 연결 확인

### Entity 및 데이터베이스 매핑

- 기존 데이터베이스 구조에 맞춰 Entity 클래스 작성
    - `User`
    - `ProductType`
    - `SystemComponent`
    - `ProductionSession`
    - `ProductDetection`
    - `Alert`
- EF Core Fluent API를 사용하여 C# Entity와 MySQL 테이블 및 컬럼 매핑
- FK 관계와 Nullable 컬럼 구조 반영
- `DbSet`을 통해 각 테이블에 접근할 수 있도록 `AppDbContext` 구성

### 로그인 및 인증 기능 구현

- API 요청/응답 데이터와 DB Entity를 분리하기 위해 DTO 구조 적용
- `LoginRequest` DTO 작성
- `BCrypt.Net-Next`를 사용하여 사용자 입력 비밀번호와 DB의 비밀번호 해시 비교
- 로그인 API 구현
    - 로그인 아이디를 기준으로 사용자 조회
    - 비밀번호 검증
    - 로그인 성공/실패 응답 처리
- Postman을 이용하여 로그인 API 동작 확인

### JWT 인증 적용

- JWT Bearer 기반 사용자 인증 기능 적용
- JWT 관련 설정값을 User Secrets로 분리
    - 서명 키
    - Issuer
    - Audience
- 로그인 성공 시 사용자 정보를 포함한 JWT 발급
    - 사용자 ID
    - 로그인 ID
    - 사용자 권한
- JWT 유효시간을 설정하고 서명, 발급자, 대상, 만료시간 검증 적용
- `Authentication`과 `Authorization` 미들웨어 구성
- Postman을 통해 JWT 인증 테스트
    - 토큰 없이 접근 시 `401 Unauthorized`
    - 유효한 Bearer Token 전달 시 정상 접근 확인

### 생산 작업 API 구현

- 생산 시작 요청을 위한 `ProductionSessionStartRequest` DTO 작성
- 로그인한 사용자의 ID를 요청 Body가 아닌 JWT에서 확인하도록 구성
- 생산 작업 시작 API 구현
    - 초콜릿 목표 세트 수 입력
    - 사탕 목표 수량 입력
    - 초기 생산 수량 0으로 설정
    - 생산 상태 `RUNNING`으로 생성
- 현재 진행 중인 생산 작업 조회 API 구현
- 생산 작업 완료 API 구현
    - 상태를 `COMPLETED`로 변경
    - 작업 종료 시간 기록
- 동일 컨베이어에서 여러 생산 작업이 동시에 진행되지 않도록 활성 작업 중복 생성 방지
    - `RUNNING` 또는 `PAUSED` 상태의 작업이 존재하면 새로운 생산 작업 생성 제한
    - 중복 생성 요청 시 `409 Conflict` 반환
- Postman과 DBeaver를 이용하여 API 응답 및 실제 DB 저장 결과 확인

### 생산 작업 운영 방식 정리

- 현재 시스템은 단일 컨베이어 라인을 기준으로 생산 작업을 관리하도록 구성
- 하나의 활성 생산 작업이 진행 중인 상태에서 새로운 생산 작업이 중복 생성되지 않도록 제한
- `RUNNING` 또는 `PAUSED` 상태의 생산 작업이 존재하면 새로운 생산 작업 시작 요청을 차단하도록 구성
- 작업자별로 생산 세션을 구분하며, 작업자 교대 시 기존 세션을 종료한 후 다음 작업자가 새로운 생산 세션을 시작하는 방식으로 운영할 예정

---

## 2026-08-11

### 알림 API 기능 확장

- 알림 생성 API에 알림 유형과 우선순위 조합 검증 로직 추가
  - `INFO`는 `LOW`만 사용 가능
  - `WARNING`은 `MEDIUM`, `HIGH` 사용 가능
  - `ERROR`는 `LOW`, `MEDIUM`, `HIGH` 사용 가능
- 잘못된 알림 유형과 우선순위 조합이 전달될 경우 DB 저장 전에 `400 Bad Request`를 반환하도록 처리
- 알림 메시지가 비어 있거나 1000자를 초과하는 경우 요청을 차단하도록 검증 로직 추가

### 알림 확인 및 복구 처리 구현

- `PATCH /api/alerts/{alertId}/check`를 통해 `WARNING`, `ERROR` 알림 확인 기능 구현
- 알림 확인 시 로그인한 사용자의 ID와 확인 시간을 저장하도록 처리
  - `check_status` → `CHECKED`
  - `checked_by_user_id` → 확인한 사용자 ID
  - `checked_at` → 확인 시간
- `INFO` 알림은 확인 처리 대상에서 제외
- 이미 확인된 알림을 다시 확인할 경우 중복 처리를 방지하도록 구성

- `PATCH /api/alerts/{alertId}/recover`를 통해 알림 복구 기능 구현
- 복구 처리 시 다음 정보를 변경하도록 구성
  - `recovery_status` → `RECOVERED`
  - `recovered_at` → 복구 시간
- `INFO` 알림은 복구 처리 대상에서 제외
- 이미 복구된 알림의 중복 복구 요청을 차단하도록 처리

### 시스템 구성요소 상태와 알림 연동

- `WARNING`, `ERROR` 알림 생성 시 해당 시스템 구성요소의 현재 상태도 함께 변경하도록 구성
  - `WARNING` 알림 → `current_status = WARNING`
  - `ERROR` 알림 → `current_status = ERROR`
- `INFO` 알림은 정보성 이력으로만 저장하고 시스템 구성요소 상태는 변경하지 않도록 처리
- 하나의 구성요소에 여러 미복구 알림이 존재할 수 있는 경우를 고려하여 복구 로직 보완
  - 미복구 `ERROR`가 남아 있으면 구성요소 상태를 `ERROR`로 유지
  - `ERROR`는 없고 미복구 `WARNING`이 남아 있으면 `WARNING`으로 유지
  - 미복구 `WARNING`, `ERROR`가 모두 없을 경우 `NORMAL`로 변경

### 생산 세션과 알림 연결

- 알림 생성 시 현재 `RUNNING` 또는 `PAUSED` 상태의 생산 세션을 조회하도록 구성
- 생산 작업 중 발생한 알림은 현재 생산 세션의 `session_id`와 자동 연결
- 생산 세션이 없는 상태에서 발생한 시스템 알림은 `session_id = NULL`로 저장하도록 처리
- Postman을 통해 생산 세션 시작 후 알림을 생성하여 현재 생산 세션 ID가 알림에 정상 연결되는 것을 확인

### 제품 감지 결과와 알림 연결 구조 확장

- `AlertCreateRequest`에 nullable `ProductDetectionId` 필드 추가
- 특정 제품 처리 과정에서 발생한 알림을 해당 `product_detections` 데이터와 연결할 수 있도록 구성
- 전달된 `ProductDetectionId`가 실제 존재하는 제품 감지 결과인지 확인하도록 검증 로직 추가
- 존재하지 않는 제품 감지 ID가 전달될 경우 `400 Bad Request`를 반환하도록 처리
- 특정 제품 감지 결과와 연결된 알림은 해당 감지 결과의 `session_id`를 사용하도록 구성하여 생산 세션 간 데이터 불일치를 방지
- 특정 제품과 관계없는 일반 장비 및 시스템 오류는 `product_detection_id = NULL`로 저장하도록 구성

### 제품 감지 결과와 알림의 역할 구분

- `product_detections.classification_status`는 제품의 분류 성공 여부를 나타내도록 역할을 명확히 구분
  - `SUCCESS` → 제품 유형 분류 성공
  - `FAILED` → 제품 유형 분류 실패
- `alerts`는 제품 처리 과정에서 발생한 장비 및 시스템 이벤트를 관리하도록 구성
- 제품 분류가 `SUCCESS`인 경우에도 이후 서보모터 또는 컨베이어 처리 과정에서 오류가 발생할 수 있으므로 제품 감지 결과와 알림 상태를 독립적으로 관리
- 특정 제품 처리와 직접 관련된 오류만 `product_detection_id`와 연결하고, 일반적인 장비 및 시스템 오류는 연결하지 않도록 기준 정리

### 제품 감지 기반 자동 알림 구조 설계

- 제품 분류 실패 시 해당 제품 감지 결과와 연결된 `ERROR` 알림을 자동 생성하는 구조 추가
- 분류 실패 알림은 `VISION_MODULE` 구성요소와 연결하도록 구성
- 분류 실패 발생 시 비전 모듈 상태를 `ERROR`로 변경하도록 처리
- 초콜릿 생산 수량이 `unit_per_set` 단위에 도달할 때마다 `INFO` 알림을 생성하도록 구성
  - 초콜릿 10개 → `1세트 생산 완료`
  - 초콜릿 20개 → `2세트 생산 완료`
- 초콜릿 세트 완료 알림은 정보성 알림으로 처리하여 `LOW` 우선순위를 사용하고 확인 및 복구 상태는 저장하지 않도록 구성

### 알림 운영 기준 정리

- 생산 시작 및 종료 이력은 `production_sessions`의 상태와 `started_at`, `ended_at`을 통해 관리하므로 별도의 `INFO` 알림은 생성하지 않도록 결정
- 단순 조회 및 일반적인 상태 확인은 알림으로 저장하지 않도록 구성
- 운영상 의미가 있는 이벤트를 중심으로 알림을 관리하도록 기준 정리
  - `INFO` : 초콜릿 세트 완료 등 주요 정보성 이벤트
  - `WARNING` : 장비 동작은 가능하지만 이상 징후가 발생한 경우
  - `ERROR` : 제품 분류 실패 및 장비·시스템 기능 수행 실패

---

## 2026-08-12

### MQTT 제품 감지 연동

- ASP.NET Core 서버와 Mosquitto MQTT Broker를 연동하였다.
- `MQTTnet`을 이용하여 MQTT Subscriber를 구현하였다.
- 서버 실행 시 MQTT Broker에 자동으로 연결되도록 구성하였다.
- MQTT로 수신한 제품 감지 데이터를 기존 `ProductDetectionService`와 연동하였다.
- REST API와 MQTT에서 동일한 제품 감지 처리 로직을 사용하도록 구성하였다.

### MQTT 제품 감지 처리 흐름

```text
Vision / MQTT Client
        ↓
Mosquitto MQTT Broker
        ↓
ASP.NET Core
MqttSubscriberService
        ↓
ProductDetectionService
        ↓
제품 감지 결과 저장
생산량 갱신
자동 알림 생성
장비 상태 갱신
```

### MQTT Topic 정리

기존 제품 감지 Topic을 전체 MQTT Topic 규칙에 맞게 변경하였다.

```text
기존
smart-sorting/product-detection

변경
smart_sorting/vision/product_detection
```

현재 큰 틀에서 정의한 주요 Topic은 다음과 같다.

| Topic | 방향 | 역할 |
|---|---|---|
| `smart_sorting/vision/product_detection` | Vision → Server | 제품 분류 결과 전달 |
| `smart_sorting/production/status` | Server → Client | 생산 현황 실시간 전달 |
| `smart_sorting/alert` | Server → Client | 실시간 알림 전달 |
| `smart_sorting/component/status` | Server → Client | 시스템 구성요소 상태 전달 |
| `smart_sorting/line/control` | Worker UI → Control | 컨베이어 제어 명령 전달 |
| `smart_sorting/line/status` | Control/Server → Worker UI | 컨베이어 상태 전달 |

### MQTT 동작 테스트

MQTT Explorer를 이용하여 다음 흐름을 테스트하였다.

```text
MQTT Explorer
        ↓
smart_sorting/vision/product_detection
        ↓
Mosquitto
        ↓
ASP.NET Core
        ↓
ProductDetectionService
        ↓
MySQL
```

- 제품 감지 MQTT 메시지 수신 확인
- 제품 감지 결과 DB 저장 확인
- 정상 분류 시 생산량 증가 확인
- 분류 실패 시 `ERROR` 알림 자동 생성 확인
- `VISION_MODULE` 상태 변경 확인
- 초콜릿 세트 생산 완료 시 `INFO` 알림 생성 확인
- 변경된 Topic인 `smart_sorting/vision/product_detection`으로 정상 수신 확인

### REST API와 MQTT 처리 구조

제품 감지 처리 로직은 `ProductDetectionService`에서 공통으로 처리한다.

```text
REST API
   ↓
ProductDetectionsController
   ┐
   ├─→ ProductDetectionService → DB
   │
MQTT
   ↓
MqttSubscriberService
   ┘
```

따라서 기존 REST API를 유지하면서 MQTT를 통한 제품 감지 입력도 사용할 수 있다.

### 작업자 UI 통신 구조 정리

작업자 UI는 REST API와 MQTT의 역할을 구분하여 사용하도록 큰 틀을 정리하였다.

#### REST API

- 로그인
- 생산 세션 시작
- 현재 생산 세션 조회
- 생산 세션 종료

#### MQTT Subscribe

```text
smart_sorting/production/status
smart_sorting/line/status
smart_sorting/alert
```

#### MQTT Publish

```text
smart_sorting/line/control
```

세부 MQTT Payload 구조와 라인 제어 처리 방식은 작업자 UI 및 장비 담당과 협의 후 확정한다.

### 관리자 웹 통신 구조 정리

관리자 웹은 조회 및 설정 기능은 REST API를 사용하고,
실시간 상태 갱신은 MQTT를 사용하는 방향으로 큰 틀을 정리하였다.

#### MQTT Subscribe

```text
smart_sorting/production/status
smart_sorting/alert
smart_sorting/component/status
```

관리자 대시보드에서 추가로 필요한 REST API는 다음과 같다.

- 오늘 생산량 요약
- 시간대별 생산량 추이
- 제품 분류 비율
- 최근 제품 감지 결과 및 이미지
- 생산 목표 조회 및 설정

세부 API 응답 구조와 MQTT Payload 구조는 관리자 웹 구현 과정에서 확정한다.

---

## 2026-08-13

### 작업자·관리자 MQTT 통신 구조 정리

- 작업자 UI와 관리자 웹에서 필요한 MQTT Topic을 구분하고 통신 구조를 정리하였다.
- 작업자와 관리자가 함께 사용하는 생산 현황 및 알림 Topic은 공통 Topic을 사용하도록 구성하였다.
- 관리자 웹에서만 사용하는 시스템 구성요소 상태 변경 Topic을 별도로 구성하였다.
- 작업자 UI에서 컨베이어 제어 명령을 전달하고 실제 장비 상태를 다시 수신하는 흐름을 정리하였다.

#### MQTT Topic 구성

| 구분 | Topic | 방향 | 용도 |
| --- | --- | --- | --- |
| 공통 | `smart_sorting/production/status` | 서버 → 작업자 / 관리자 | 생산 현황 실시간 전달 |
| 공통 | `smart_sorting/alert` | 서버 → 작업자 / 관리자 | 신규 알림 실시간 전달 |
| 작업자 | `smart_sorting/line/control` | 작업자 → 제어 장치 | 컨베이어 시작·정지·속도 제어 |
| 작업자 | `smart_sorting/line/status` | 제어 장치 → 작업자 | 실제 컨베이어 동작 상태 전달 |
| 관리자 | `smart_sorting/component/status` | 서버 → 관리자 | 시스템 구성요소 상태 변경 전달 |
| 서버 입력 | `smart_sorting/vision/product_detection` | Vision → 서버 | 제품 감지 결과 전달 |

### 생산 현황 MQTT Payload 정리

- 초콜릿과 사탕의 현재 생산량, 목표 생산량, 세트 구성 수량, 완료 세트 수, 진행률을 전달하도록 구성하였다.
- 초콜릿의 목표 개수는 목표 세트 수에 `unitPerSet`을 곱해 계산한다.
- 사탕은 1개가 1세트이므로 `unitPerSet = 1`을 사용한다.
- `unitPerSet`은 `product_types` 테이블의 값을 사용하도록 수정하였다.

```json
{
  "sessionId": 12,
  "status": "RUNNING",
  "chocolate": {
    "currentCount": 20,
    "targetCount": 100,
    "unitPerSet": 10,
    "setCount": 2,
    "progress": 20
  },
  "candy": {
    "currentCount": 35,
    "targetCount": 100,
    "unitPerSet": 1,
    "setCount": 35,
    "progress": 35
  }
}
```

### 생산 현황 MQTT Publish 구현

- 제품 분류 성공 시 생산량 변경 결과를 `smart_sorting/production/status` Topic으로 Publish하도록 구현하였다.
- 기존 REST 제품 감지 기능은 유지하고, REST와 MQTT 입력이 동일한 제품 감지 처리 로직을 사용하도록 구성하였다.
- 초콜릿과 사탕의 생산 수량 및 진행률이 실제 DB 값 기준으로 계산되도록 하였다.

### 알림 MQTT Publish 구현

- 제품 분류 과정에서 새 알림이 생성되면 `smart_sorting/alert` Topic으로 전달하도록 구현하였다.
- 초콜릿 세트 생산 완료 시 INFO 알림을 생성하고 전달한다.
- 제품 분류 실패 시 ERROR 알림을 생성하고 전달한다.
- 관리자와 작업자가 동일한 알림 Payload를 사용할 수 있도록 공통 형식으로 정리하였다.

```json
{
  "alertId": 15,
  "alertType": "ERROR",
  "priority": "MEDIUM",
  "componentCode": "VISION_MODULE",
  "alertMessage": "제품 분류에 실패했습니다.",
  "createdAt": "..."
}
```

### 시스템 구성요소 상태 MQTT Publish 구현

- 시스템 구성요소 상태 변경 시 `smart_sorting/component/status` Topic으로 변경된 상태를 전달하도록 구현하였다.
- 시스템 구성요소 상태 API를 통한 수동 상태 변경 시 MQTT Publish가 정상 동작하는 것을 확인하였다.

```json
{
  "componentCode": "CAMERA",
  "status": "ERROR"
}
```

### 제품 분류 실패 시 VISION_MODULE 상태 자동 변경

- 제품 분류에 실패하면 `VISION_MODULE` 상태를 `ERROR`로 변경하도록 구현하였다.
- 기존 상태가 `ERROR`가 아닌 경우에만 실제 상태 변경으로 판단하여 `component/status` MQTT 메시지를 전달한다.
- 이미 `ERROR` 상태인 경우 중복 상태 메시지를 전달하지 않도록 처리하였다.

```text
제품 분류 FAILED
        ↓
ERROR 알림 생성
        ↓
VISION_MODULE 상태 확인
        ↓
NORMAL → ERROR
        ↓
component/status MQTT Publish
```

### 알림 복구에 따른 장비 상태 재계산 및 MQTT Publish

- WARNING 또는 ERROR 알림 복구 시 해당 시스템 구성요소에 남아 있는 미복구 알림을 다시 확인하도록 구현하였다.
- 같은 장비에 미복구 ERROR가 있으면 `ERROR` 상태를 유지한다.
- ERROR는 없고 미복구 WARNING이 있으면 `WARNING` 상태로 변경한다.
- 미복구 WARNING과 ERROR가 모두 없으면 `NORMAL` 상태로 변경한다.
- 실제 구성요소 상태가 변경된 경우 `component/status` MQTT 메시지를 전달하도록 구현하였다.

```text
ERROR 알림 Recover
        ↓
같은 장비의 미복구 알림 확인
        ↓
ERROR 존재   → ERROR
WARNING 존재 → WARNING
둘 다 없음   → NORMAL
        ↓
상태 변경 시 MQTT Publish
```

### 관리자 웹 MQTT WebSocket 연동

- 브라우저 기반 관리자 웹에서 MQTT 메시지를 수신할 수 있도록 Mosquitto WebSocket Listener를 추가하였다.
- 기존 일반 MQTT 통신은 `1883` 포트를 유지하고, 브라우저용 WebSocket 포트로 `9001`을 사용하도록 구성하였다.
- Windows 방화벽에서 TCP `9001` 인바운드 허용 규칙을 추가하였다.
- 공유기에서 외부 `9001` 포트를 서버 PC의 `9001` 포트로 포트포워딩하였다.
- 외부 PC에서 `Test-NetConnection`을 통해 `9001` 포트 연결을 확인하였다.

#### 관리자 웹 연결 구조

```text
ASP.NET Core / Raspberry Pi / MQTT Explorer
        ↓
    MQTT TCP 1883
        ↓
     Mosquitto
        ↑
 WebSocket 9001
        ↑
    관리자 웹
```

---

## 현재 완료 상태

- [x] 데이터베이스 생성 스크립트
- [x] 6개 테이블 정의
- [x] 기본 키·외래키·고유 키 정의
- [x] NULL 허용 여부 정의
- [x] CHECK 제약조건 정의
- [x] 제품 유형 초기 데이터
- [x] 시스템 구성요소 초기 데이터
- [x] 테스트용 더미 데이터
- [x] DBeaver ERD
- [x] ERDCloud ERD
- [x] ASP.NET Core Web API 프로젝트 생성
- [x] MySQL 및 Entity Framework Core 연동
- [x] Entity 모델 및 `AppDbContext` 작성
- [x] BCrypt 기반 로그인 API 구현
- [x] JWT 발급 및 인증 처리
- [x] 생산 작업 시작 API
- [x] 현재 생산 작업 조회 API
- [x] 생산 작업 종료 API
- [x] 생산 작업 종료 시 목표 달성 여부 판단
- [x] 활성 생산 작업 중복 생성 방지
- [x] 제품 감지 결과 저장 API
- [x] 제품 분류 성공·실패 검증
- [x] 초콜릿·사탕 생산 수량 갱신 로직
- [x] 시스템 구성요소 상태 조회 API
- [x] 시스템 구성요소 상태 변경 API
- [x] 알림 생성 API
- [x] 알림 조회 API
- [x] 알림 확인 처리 API
- [x] 알림 복구 처리 API
- [x] 알림 유형 및 우선순위 조합 검증
- [x] 알림과 시스템 구성요소 상태 연동
- [x] 알림과 현재 생산 세션 연결
- [x] 알림과 제품 감지 결과 연결 구조 구현
- [x] 동일 구성요소의 미복구 알림을 고려한 상태 복구 처리
- [x] 제품 분류 실패 시 자동 `ERROR` 알림 생성
- [x] 초콜릿 세트 완료 시 `INFO` 알림 생성
- [x] 제품 감지 처리 로직 `ProductDetectionService` 분리
- [x] 외부 HTTP API 접속 확인
- [x] Mosquitto 외부 접속 확인
- [x] MQTT Broker와 ASP.NET Core 서버 연결
- [x] MQTT 제품 감지 Subscribe 구현
- [x] MQTT 제품 감지 결과 DB 저장 및 생산량 갱신 테스트
- [x] MQTT 제품 감지 Topic 이름 정리
- [x] 작업자 UI 통신 구조 큰 틀 정리
- [x] 관리자 웹 통신 구조 큰 틀 정리
- [x] 작업자·관리자 공통 MQTT Payload 구조 정리
- [x] 서버 → 클라이언트 MQTT Publish 구현
- [x] `smart_sorting/production/status` Publish 구현
- [x] `smart_sorting/alert` Publish 구현
- [x] `smart_sorting/component/status` Publish 구현
- [x] 제품 분류 실패 시 `VISION_MODULE` `ERROR` 자동 변경
- [x] 제품 분류 실패에 따른 `component/status` MQTT Publish
- [x] 알림 복구 시 시스템 구성요소 상태 재계산
- [x] 알림 복구에 따른 `component/status` MQTT Publish
- [x] Mosquitto WebSocket `9001` Listener 구성
- [x] 외부 네트워크에서 WebSocket 포트 연결 확인
- [ ] 관리자 웹 MQTT WebSocket 연결 확인
- [ ] 관리자 웹 MQTT 메시지 수신 확인
- [ ] 수동 알림 생성 시 MQTT Publish 연동
- [ ] 생산 작업 시작·종료 시 `production/status` MQTT Publish 연동
- [ ] 관리자 대시보드 통계 API
- [ ] 작업자 UI 라인 제어 MQTT 연동
- [ ] Raspberry Pi 및 클라이언트 통합 테스트

---

## 다음 작업

1. `AlertsController`의 수동 알림 생성 시 MQTT Publish 기능을 추가한다.
    - `smart_sorting/alert`
    - 알림 생성으로 구성요소 상태가 변경되는 경우 `smart_sorting/component/status`

2. 생산 작업 시작·종료 시 생산 상태 변경 내용을 MQTT로 전달하도록 연동한다.
    - `smart_sorting/production/status`

3. 관리자 대시보드용 통계 API를 구현한다.
    - 오늘 생산량 요약
    - 시간대별 생산량 추이
    - 제품 분류 비율
    - 최근 제품 감지 결과 및 이미지

4. 작업자 UI의 라인 제어 처리 구조를 장비 담당과 최종 확정하고 MQTT Topic을 연동한다.
    - `smart_sorting/line/control`
    - `smart_sorting/line/status`

5. 작업자 UI와 관리자 웹에 실제 MQTT 데이터를 연결해 화면 반영을 확인한다.

6. Raspberry Pi, 작업자 UI, 관리자 웹과 실제 통합 테스트를 진행한다.
