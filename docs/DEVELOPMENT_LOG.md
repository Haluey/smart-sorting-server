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

## 2026-08-14

### 수동 알림 생성 MQTT Publish 구현

- `AlertsController`의 수동 알림 생성 API와 MQTT Publish 기능을 연동하였다.
- 새로운 알림이 생성되면 `smart_sorting/alert` Topic으로 알림 정보를 전달하도록 구현하였다.
- WARNING 또는 ERROR 알림 생성으로 시스템 구성요소 상태가 실제로 변경된 경우 `smart_sorting/component/status` Topic으로 변경된 상태를 전달하도록 구현하였다.
- 기존 상태와 동일한 상태로 변경되는 경우 불필요한 `component/status` 메시지를 중복 전송하지 않도록 처리하였다.

#### 알림 생성 흐름

```text
POST /api/alerts
        ↓
알림 DB 저장
        ↓
smart_sorting/alert Publish
        ↓
구성요소 상태 변경 여부 확인
        ↓
상태가 변경된 경우
smart_sorting/component/status Publish
```

### 수동 알림 MQTT 테스트

- 시스템 구성요소 상태를 `NORMAL`로 설정한 후 WARNING 알림을 생성하여 테스트하였다.
- 신규 WARNING 알림이 `smart_sorting/alert` Topic으로 정상 전달되는 것을 확인하였다.
- 구성요소 상태가 `NORMAL → WARNING`으로 변경되면서 `smart_sorting/component/status` Topic으로 상태 변경 메시지가 전달되는 것을 확인하였다.
- 동일 상태의 알림이 반복 발생하는 경우 알림 메시지만 새로 전달하고 구성요소 상태 메시지는 중복 Publish하지 않도록 구성하였다.

### 생산 작업 상태 MQTT Publish 구현

- 생산 작업 시작 및 종료 시 현재 생산 상태를 `smart_sorting/production/status` Topic으로 전달하도록 구현하였다.
- 기존 제품 분류 성공 시 생산량 변경 Publish와 동일한 Payload 구조를 사용하도록 구성하였다.
- 초콜릿과 사탕의 현재 수량, 목표 수량, 세트 구성 수량, 완료 세트 수 및 진행률을 함께 전달하도록 하였다.

#### 생산 작업 시작

생산 작업 생성 후 `RUNNING` 상태와 초기 생산 정보를 전달한다.

```json
{
  "sessionId": 12,
  "status": "RUNNING",
  "chocolate": {
    "currentCount": 0,
    "targetCount": 20,
    "unitPerSet": 10,
    "setCount": 0,
    "progress": 0
  },
  "candy": {
    "currentCount": 0,
    "targetCount": 3,
    "unitPerSet": 1,
    "setCount": 0,
    "progress": 0
  }
}
```

#### 생산 작업 종료

- 생산 목표를 모두 달성한 경우 `COMPLETED` 상태를 전달한다.
- 목표를 달성하지 못한 상태에서 종료한 경우 `CANCELLED` 상태를 전달한다.
- 종료 시점의 최종 생산량과 진행률을 함께 전달하도록 구현하였다.

```text
생산 시작
→ RUNNING Publish

제품 분류 성공
→ 생산량 변경 Publish

생산 종료
→ COMPLETED / CANCELLED Publish
```

### MQTT 서버 기능 정리

서버에서 사용하는 주요 MQTT 기능을 구현하였다.

| Topic | 방향 | 용도 |
| --- | --- | --- |
| `smart_sorting/vision/product_detection` | Vision → 서버 | 제품 감지 결과 수신 |
| `smart_sorting/production/status` | 서버 → 작업자 / 관리자 | 생산 현황 및 상태 변경 전달 |
| `smart_sorting/alert` | 서버 → 작업자 / 관리자 | 신규 알림 전달 |
| `smart_sorting/component/status` | 서버 → 관리자 | 시스템 구성요소 상태 변경 전달 |

현재 서버에서는 다음 상황에 MQTT 메시지를 전달하도록 구성되어 있다.

- 생산 작업 시작
- 제품 분류 성공 및 생산량 변경
- 생산 작업 종료
- 자동 INFO / ERROR 알림 생성
- 수동 알림 생성
- 시스템 구성요소 상태 변경
- 제품 분류 실패에 따른 `VISION_MODULE` 오류 상태 변경
- 알림 복구에 따른 시스템 구성요소 상태 재계산

### 개발 환경 오류 해결

- Docker MySQL 컨테이너 실행 과정에서 호스트 `3306` 포트 바인딩 오류가 발생하였다.
- Windows에서 `netstat` 및 `Get-NetTCPConnection`을 이용해 포트 사용 상태를 확인하였다.
- 시스템 재부팅 후 Docker 및 MySQL 컨테이너를 다시 실행하여 데이터베이스 연결 문제를 해결하였다.
- 이후 API 및 MQTT 테스트를 정상적으로 진행하였다.

---

## 2026-08-18 — 생산 목표 관리 구조 개선 및 웹·Qt REST API 역할 분리

### 관리자 웹 CORS 설정

- 관리자 웹 개발 환경에서 ASP.NET Core REST API를 호출할 수 있도록 CORS 정책을 추가하였다.
- 다음 개발 Origin을 허용하도록 설정하였다.
  - `http://127.0.0.1:5500`
  - `http://localhost:5500`
- `Program.cs`에 `AddCors()`와 `UseCors()`를 적용하였다.
- 관리자 웹에서 JWT Bearer Token을 이용해 인증이 필요한 REST API를 호출할 수 있도록 서버 접근 환경을 정리하였다.

### 관리자 계정 BCrypt 해시 적용

- 관리자 테스트 계정의 임시 비밀번호 값을 BCrypt 해시값으로 변경하였다.
- 서버에서 `BCrypt.Net.BCrypt.HashPassword()`를 이용하여 테스트 비밀번호의 해시값을 생성하였다.
- 생성한 해시값을 `users.password_hash`에 저장하여 작업자 계정과 동일한 방식으로 로그인할 수 있도록 정리하였다.

### 생산 목표 관리 구조 분리

- 기존에는 생산 작업 시작 요청과 함께 목표 생산량을 전달하는 구조였다.
- 관리자 웹과 작업자 Qt의 역할을 분리하기 위해 생산 목표 설정과 생산 시작 기능을 분리하였다.
- 관리자 웹에서는 생산 목표만 설정하고 실제 생산 시작·종료는 작업자 Qt에서 처리하도록 구조를 변경하였다.
- 관리자 웹이 종료되어 있어도 작업자 Qt가 서버에 저장된 목표값을 이용해 생산을 시작할 수 있도록 생산 목표를 DB에서 관리하기로 결정하였다.

```text
관리자 웹
        ↓
생산 목표 설정
        ↓
production_targets 저장
        ↓
작업자 Qt에서 생산 시작
        ↓
서버가 현재 생산 목표 조회
        ↓
production_sessions에 목표값 복사
        ↓
생산 시작
```

### production_targets 테이블 추가

- 현재 생산 목표를 별도로 관리하기 위해 `production_targets` 테이블을 추가하였다.
- 현재 목표값만 저장하는 용도이므로 `target_id = 1`인 한 개의 행을 유지하도록 구성하였다.
- 관리자가 새로운 목표를 설정할 때 새 행을 추가하지 않고 기존 행을 `UPDATE`하도록 정리하였다.
- 테이블 컬럼은 다음과 같이 구성하였다.
  - `target_id`
  - `target_chocolate_set_count`
  - `target_candy_count`
  - `updated_at`
- 현재 실행 중인 생산 세션의 목표값은 변경하지 않고, 수정된 목표는 다음 생산 세션 시작 시 적용하도록 구성하였다.

### ProductionTarget 모델 및 EF Core 매핑

- `ProductionTarget` Entity를 추가하였다.
- 기존 Entity 작성 방식과 동일하게 단순 POCO 형태로 작성하였다.
- `AppDbContext`에 `ProductionTargets` DbSet을 추가하였다.

```csharp
public DbSet<ProductionTarget> ProductionTargets { get; set; } = null!;
```

- Fluent API를 이용하여 `ProductionTarget` 모델과 `production_targets` 테이블을 매핑하였다.
- 다음 프로퍼티와 DB 컬럼을 연결하였다.
  - `TargetId` → `target_id`
  - `TargetChocolateSetCount` → `target_chocolate_set_count`
  - `TargetCandyCount` → `target_candy_count`
  - `UpdatedAt` → `updated_at`

### 생산 목표 조회·설정 REST API 구현

- 관리자 웹에서 현재 생산 목표를 조회하고 수정할 수 있도록 `ProductionTargetsController`를 추가하였다.
- 현재 목표 조회 API를 구현하였다.

```text
GET /api/production-targets/current
```

- `target_id = 1`인 현재 생산 목표를 조회하여 초콜릿 목표 세트 수, 사탕 목표 수량, 수정 시간을 반환하도록 구성하였다.
- 생산 목표 설정 API를 구현하였다.

```text
PUT /api/production-targets/current
```

- 요청으로 전달받은 초콜릿 목표 세트 수와 사탕 목표 수량을 기존 `target_id = 1` 행에 반영하도록 구성하였다.
- 목표 생산량이 음수인 경우 `400 Bad Request`를 반환하도록 검증 로직을 추가하였다.
- 생산 목표 조회 및 수정 API가 정상 동작하는 것을 확인하였다.

### 작업자 Qt 생산 시작 API 구조 수정

- 작업자 Qt에서 사용하는 `POST /api/production-sessions/start` API의 구조를 수정하였다.
- 기존 `ProductionSessionStartRequest` DTO를 통해 목표값을 전달하던 방식을 제거하였다.
- 작업자 Qt는 생산 시작 시 Request Body를 전달하지 않도록 변경하였다.
- 서버가 `production_targets`의 현재 목표값을 직접 조회하도록 구성하였다.
- 조회한 목표값을 새로 생성되는 `production_sessions`에 복사하여 해당 생산 작업의 목표로 저장하도록 하였다.
- 생산 시작 당시 복사된 목표값은 이후 관리자가 생산 목표를 변경해도 유지되도록 구성하였다.

```text
POST /api/production-sessions/start
        ↓
production_targets 조회
        ↓
현재 목표값 확인
        ↓
production_sessions 생성
        ↓
목표값 복사
        ↓
RUNNING
```

### 생산 시작 사용자 인증 오류 수정

- 생산 시작 API에서 JWT 사용자 ID를 `"userId"` Claim 이름으로 직접 조회하면서 사용자 정보를 찾지 못하는 문제가 발생하였다.
- 기존 `GetCurrentProduction()`과 동일하게 `ClaimTypes.NameIdentifier`를 이용해 사용자 ID를 조회하도록 수정하였다.

```csharp
var userIdValue = User.FindFirstValue(
    ClaimTypes.NameIdentifier
);
```

- 수정 후 작업자 JWT를 이용한 생산 시작 API가 정상 동작하는 것을 확인하였다.

### 작업자 Qt REST API 흐름 정리

- 작업자 Qt에서 사용하는 REST API의 역할을 실제 작업 흐름에 맞게 정리하였다.
- 로그인 후 현재 진행 중인 작업이 있는지 확인하기 위해 현재 생산 작업 조회 API를 유지하였다.
- Qt가 비정상 종료되거나 다시 실행되는 경우 서버에 남아 있는 `RUNNING` 또는 `PAUSED` 생산 세션을 확인할 수 있도록 하였다.

```text
POST /api/auth/login
        ↓
GET /api/production-sessions/current
        ↓
진행 중인 작업 존재
→ 기존 생산 작업 사용

진행 중인 작업 없음
→ POST /api/production-sessions/start
```

- 로그아웃 시 별도의 로그아웃 API를 사용하지 않고 `PATCH /api/production-sessions/finish`를 호출하도록 정리하였다.
- 생산 종료 후 Qt에 저장된 JWT를 제거하고 로그인 화면으로 이동하는 흐름으로 구성하였다.

### 관리자 웹과 작업자 Qt REST API 역할 분리

- 관리자 웹과 작업자 Qt에서 사용하는 REST API의 역할을 구분하였다.

#### 관리자 웹

- 로그인
- 대시보드 조회
- 최근 제품 감지 결과 조회
- 알림 조회·확인·복구
- 시스템 구성요소 상태 조회
- 현재 생산 목표 조회
- 생산 목표 설정

#### 작업자 Qt

- 로그인
- 현재 생산 작업 조회
- 생산 시작
- 로그아웃 시 생산 종료

- 관리자 웹은 모니터링 및 생산 목표 관리에 집중하고, 작업자 Qt는 실제 생산 작업의 시작과 종료를 담당하도록 역할을 정리하였다.

### 생산 시작 MQTT Publish 유지

- 생산 시작 API 구조 변경 이후에도 기존 `smart_sorting/production/status` MQTT Publish 기능을 유지하였다.
- 새 생산 세션 생성 후 `RUNNING` 상태와 초기 생산 정보를 전달하도록 구성하였다.
- 초콜릿 목표 수량은 목표 세트 수와 `unit_per_set`을 이용하여 낱개 수로 계산하도록 하였다.
- 생산 시작 시 현재 생산량, 완료 세트 수, 진행률은 0으로 전달하도록 구성하였다.

```text
POST /api/production-sessions/start
        ↓
production_sessions 생성
        ↓
smart_sorting/production/status Publish
```

### 제품 분류 실패 시 VISION_MODULE MQTT 처리 수정

- 제품 분류 실패 시 `VISION_MODULE`의 상태를 `ERROR`로 저장하는 기존 DB 처리 방식은 유지하였다.
- 분류 실패와 연결된 `ERROR` 알림 생성 및 `smart_sorting/alert` MQTT Publish도 유지하였다.
- 다만 제품 분류 실패를 실제 장비 상태 변경 메시지처럼 관리자 웹에 전달하지 않도록 `smart_sorting/component/status` MQTT Publish를 제거하였다.
- `visionStatusChanged` 변수와 관련 상태 변경 확인 로직도 함께 제거하였다.

```text
제품 분류 FAILED
        ↓
product_detections FAILED 저장
        ↓
VISION_MODULE 상태 ERROR DB 저장
        ↓
ERROR 알림 DB 저장
        ↓
smart_sorting/alert Publish
```

- 제품 분류 실패에 따른 `VISION_MODULE` 상태는 DB에는 기록하지만, `smart_sorting/component/status` Topic으로는 전달하지 않도록 구조를 수정하였다.

### VISION_MODULE 오류 처리 테스트

- 제품 감지 실패 데이터를 이용하여 수정된 처리 흐름을 테스트하였다.
- `product_detections`에 `FAILED` 결과가 정상 저장되는 것을 확인하였다.
- `system_components`의 `VISION_MODULE` 상태가 `ERROR`로 정상 변경되는 것을 확인하였다.
- 연결된 `ERROR` 알림이 DB에 정상 저장되는 것을 확인하였다.
- `smart_sorting/alert` MQTT 메시지가 정상 전달되는 것을 확인하였다.
- 제품 분류 실패 시 `smart_sorting/component/status` MQTT 메시지가 더 이상 전달되지 않는 것을 확인하였다.

```text
제품 감지 FAILED 저장               → 정상
VISION_MODULE 상태 ERROR 저장       → 정상
ERROR 알림 저장                     → 정상
smart_sorting/alert Publish         → 정상
smart_sorting/component/status      → Publish 안 함
```

---

## 2026-08-19 — 생산 목표 구조 검증 및 서버 로그 출력 개선

### 생산 목표 구조 통합 테스트

- 전날 변경한 생산 목표 관리 구조와 생산 시작 흐름을 실제 REST API로 다시 확인하였다.
- `production_targets`에 설정된 목표값이 생산 시작 시 새로운 `production_sessions`에 정상 복사되는 것을 확인하였다.
- 생산 중 관리자가 목표값을 변경해도 현재 진행 중인 생산 세션의 목표값은 유지되는 것을 확인하였다.
- 현재 생산 세션 종료 후 다음 생산 세션부터 변경된 목표값이 적용되는 것을 확인하였다.
- 작업자 Qt가 생산 목표를 직접 전달하지 않고 서버에 저장된 현재 목표를 사용하는 구조가 정상 동작하는 것을 확인하였다.

### 실제 장비 MQTT 연동 구조 정리

- 실제 센서·장비와 ASP.NET Core 서버가 직접 맞춰야 하는 MQTT 통신 구조를 정리하였다.
- 비전 제품 감지 결과는 기존 Topic을 유지하기로 하였다.

```text
smart_sorting/vision/product_detection
```

- 실제 장비 상태를 서버에 전달하기 위한 입력 Topic을 다음과 같이 정리하였다.

```text
smart_sorting/component/status/update
```

- 실제 장비 상태 Payload는 다음 구조를 기준으로 사용하기로 하였다.

```json
{
  "componentCode": "CAMERA",
  "status": "ERROR",
  "message": "카메라 촬영에 실패했습니다."
}
```

- Payload 필드 역할:
  - `componentCode` → 필수
  - `status` → 필수
  - `message` → 선택
- `message`는 `WARNING`, `ERROR`, `OFFLINE` 상태에서 실제 오류 내용을 `alerts.alert_message`에 저장할 수 있도록 전달하는 것을 권장하기로 하였다.

### 실제 장비 상태와 알림 처리 기준 정리

- 실제 장비 상태는 서버가 먼저 수신하여 `system_components`의 현재 상태를 갱신하도록 구조를 정리하였다.
- 장비에서 이상 상태가 전달되면 `alerts`에도 관련 데이터를 저장하도록 기준을 정리하였다.

```text
NORMAL
→ system_components 상태 갱신
→ Alert 생성 안 함

WARNING
→ system_components = WARNING
→ WARNING Alert 생성

ERROR
→ system_components = ERROR
→ ERROR Alert 생성

OFFLINE
→ system_components = OFFLINE
→ 필요 시 WARNING 또는 ERROR Alert 생성
```

- 서버에서 처리한 장비 상태와 알림은 기존 MQTT Topic을 이용하여 관리자 웹에 전달하도록 정리하였다.

```text
장비 → 서버
smart_sorting/component/status/update

서버 → 관리자 웹
smart_sorting/component/status
smart_sorting/alert
```

### 실제 장비 componentCode 정리

- 실제 장비 상태 MQTT에서 사용할 주요 `componentCode`를 정리하였다.

```text
IR_SENSOR
→ 적외선 센서

CAMERA
→ 카메라

CONVEYOR
→ 컨베이어

SORTING_SERVO
→ 분류 서보모터
```

- 작업자용 LCD는 `WORKER_DISPLAY`라는 `componentCode`를 사용하기로 결정하였다.
- `WORKER_DISPLAY`는 아직 실제 `system_components` 데이터에 반영하지 않았다.
- 장비에서 전달하는 `componentCode`는 서버 DB의 `system_components.component_code`와 동일한 값을 사용하도록 정리하였다.

### NLog 적용 및 콘솔 로그 출력 개선

- Visual Studio에서 서버 실행 시 EF Core SQL 로그와 ASP.NET Core 내부 로그가 과도하게 출력되는 문제를 개선하였다.
- `appsettings.Development.json`에서 EF Core SQL Command 로그 수준을 `Warning`으로 조정하여 불필요한 SQL 로그 출력을 줄였다.
- 서버 콘솔 로그를 간단하게 관리하기 위해 `NLog.Web.AspNetCore` 패키지를 설치하였다.
- 프로젝트에 `nlog.config`를 추가하고 Console Target의 출력 형식을 `${message}`로 설정하였다.

```xml
<target xsi:type="Console"
        name="console"
        layout="${message}" />
```

- `Program.cs`에 NLog를 연결하였다.

```csharp
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Host.UseNLog();
```

- 적용 후 기존 `info: Microsoft.Hosting.Lifetime[...]` 형식 대신 실제 메시지만 출력되도록 변경하였다.
- 서버 실행 여부와 포트, 실행 환경을 확인하기 위해 ASP.NET Core 시작 로그는 유지하기로 하였다.

```text
Now listening on: https://localhost:5050
Now listening on: http://0.0.0.0:5051
Application started. Press Ctrl+C to shut down.
Hosting environment: Development
Content root path: C:\Workspace\smart-sorting-server\src\SmartSortingServer
```

### MQTT 연결 로그 출력 순서 개선

- 기존에는 MQTT 연결 로그가 ASP.NET Core 시작 로그보다 먼저 출력되었다.
- `MqttSubscriberService`에 `IHostApplicationLifetime`을 추가하고 `ApplicationStarted` 신호를 기다린 뒤 MQTT Broker에 연결하도록 수정하였다.
- 임의의 대기 시간을 사용하지 않고 실제 서버 시작 완료 시점을 기준으로 MQTT 연결을 시작하도록 구성하였다.

```csharp
await WaitForApplicationStartedAsync(
    stoppingToken
);
```

- 수정 후 서버 시작 로그가 먼저 출력되고, 그 아래에 MQTT 연결 및 Topic 구독 로그가 출력되는 것을 확인하였다.

```text
Now listening on: https://localhost:5050
Now listening on: http://0.0.0.0:5051
Application started. Press Ctrl+C to shut down.
Hosting environment: Development
Content root path: C:\Workspace\smart-sorting-server\src\SmartSortingServer

MQTT Broker 연결 성공
MQTT Topic 구독 완료: smart_sorting/vision/product_detection
```

### 추가 로그 출력 문제 확인

- REST API 호출 시 ASP.NET Core Routing, MVC, Endpoint 및 EF Core 내부 Information 로그가 다시 출력되는 것을 확인하였다.
- `nlog.config`의 전체 Information 로그 출력 규칙이 원인으로 확인되었다.
- `Microsoft.Hosting.Lifetime` 시작 로그는 유지하면서 내부 프레임워크 로그는 숨기도록 NLog Rule을 추가로 정리할 필요가 있다.
- 해당 NLog Rule 수정은 아직 적용하지 않았다.

---

## 2026-08-24

### 서버 로그 구조 정리

- NLog 설정을 정리하여 ASP.NET Core 및 EF Core의 불필요한 `Information` 로그를 숨기도록 수정
- 서버에서 직접 작성한 업무 로그만 콘솔에서 확인할 수 있도록 구성
- 콘솔 로그 형식은 `${message}`를 유지하여 불필요한 시간 및 로그 레벨 표시는 제외
- 업무 영역별 로그 Prefix를 통일

```text
[LOGIN]
[TARGET]
[SESSION]
[DETECTION]
[ALERT]
[COMPONENT]
[MQTT]
```

### 로그인 로그 추가

- 로그인 성공 시 사용자 로그인 ID를 로그로 출력

```text
[LOGIN] 2601 로그인 성공
```

### 생산 목표 변경 로그 추가

- 생산 목표 변경 완료 후 초콜릿 세트 수와 사탕 목표 수량을 로그로 출력
- 생산 목표가 0 이하인 경우 저장되지 않도록 검증 조건 수정

```text
[TARGET] 생산 목표 변경 - ChocolateSet: 10, Candy: 100
```

### 생산 작업 로그 추가

- 생산 작업 시작 시 `SessionId`, `UserId` 출력
- 생산 작업 종료 시 `SessionId`, 최종 상태 출력

```text
[SESSION] 생산 작업 시작 - SessionId: 20, UserId: 2
[SESSION] 생산 작업 종료 - SessionId: 20, Status: CANCELLED
```

### 제품 분류 로그 추가

- 제품 분류 성공 시 `DetectionId`, 제품 유형, Confidence 출력
- 제품 분류 실패 시 `LogError()` 사용
- 성공/실패 모두 `[DETECTION]` Prefix로 통일

```text
[DETECTION] 제품 분류 성공 - DetectionId: 107, ProductType: CHOCOLATE, Confidence: 0.95
[DETECTION] 제품 분류 실패 - DetectionId: 108
```

### MQTT 로그 정리

- MQTT Publish 시 전체 Payload 출력을 제거하고 Topic만 출력하도록 수정
- MQTT Receive 시 전체 Payload 출력을 제거하고 Topic만 출력하도록 수정
- MQTT 연결 실패, 데이터 변환 실패, 제품 감지 처리 실패 로그를 `ILogger` 기반으로 변경
- 제품 감지 저장 완료 로그는 `[DETECTION]` 로그와 중복되어 제거
- MQTT Broker 연결 성공 및 Topic 구독 완료 메시지는 서버 시작 상태 확인용으로 유지

```text
[MQTT] Receive - Topic: smart_sorting/vision/product_detection
[MQTT] Publish - Topic: smart_sorting/production/status
[MQTT] Publish - Topic: smart_sorting/alert
[MQTT] Publish - Topic: smart_sorting/component/status
```

### 알림 로그 추가

- 알림 생성 시 `AlertId`, 알림 유형, 우선순위, 구성요소 코드 출력
- 알림 확인 처리 시 `AlertId`, 확인 사용자 ID 출력
- 알림 복구 처리 시 `AlertId` 출력

```text
[ALERT] 알림 생성 - AlertId: 38, Type: ERROR, Priority: MEDIUM, Component: VISION_MODULE
[ALERT] 알림 확인 - AlertId: 38, UserId: 2
[ALERT] 알림 복구 - AlertId: 38
```

### 시스템 구성요소 상태 변경 로그 추가

- `SystemComponentsController`에서 장비 상태가 실제로 변경된 경우에만 `[COMPONENT]` 로그 출력
- 알림 생성으로 인해 장비 상태가 변경되는 경우에도 `[COMPONENT]` 로그 출력
- 알림 복구로 인해 장비 상태가 변경되는 경우에도 `[COMPONENT]` 로그 출력
- 상태가 동일한 경우에는 MQTT `component/status`를 Publish하지 않도록 기존 조건 유지

```text
[COMPONENT] 상태 변경 - Component: VISION_MODULE, NORMAL -> ERROR
[COMPONENT] 상태 변경 - Component: VISION_MODULE, ERROR -> NORMAL
```

### 알림 생성/확인/복구 동작 테스트

- ERROR 알림 생성 시 `alerts` 저장 및 `[ALERT]` 로그 확인
- 구성요소 상태가 `NORMAL -> ERROR`로 변경되는 경우 `component/status` MQTT Publish 확인
- 알림 확인 처리 시 `CHECKED` 상태 및 확인 사용자 정보 저장 확인
- 알림 복구 처리 시 같은 장비의 미복구 ERROR/WARNING 존재 여부를 확인한 뒤 구성요소 상태 재계산
- 미복구 알림이 없는 경우 `ERROR -> NORMAL` 복구 및 MQTT Publish 확인

### 제어부 MQTT 메시지 규격 검토

제어부에서 전달한 MQTT 규격을 검토하였다.

#### 제품 분류 결과 Topic

```text
smart_sorting/vision/product_detection
```

- 제품 단위의 분류 결과를 전달
- `SUCCESS` / `FAILED`, 제품 종류, Confidence, 이미지 경로 사용
- 제품 처리 중 장비 오류가 발생한 경우 해당 제품의 처리 결과는 `FAILED`가 될 수 있음

#### 구성요소 상태 Topic

```text
smart_sorting/component/status/update
```

- 장비 및 통신 상태를 별도로 전달
- 제품 분류 결과와 장비 상태 정보를 서로 다른 의미로 처리

예정 Payload:

```json
{
  "componentCode": "CAMERA",
  "status": "ERROR",
  "eventCode": "CAMERA_ERROR",
  "message": "Camera capture failed"
}
```

- `componentCode`: 오류가 발생한 장비
- `status`: `NORMAL`, `WARNING`, `ERROR`, `OFFLINE`
- `eventCode`: 서버에서 이벤트 종류를 구분하기 위한 코드
- `message`: 상세 설명

### 제품 분류 실패와 장비 상태 처리 방향 정리

- `vision/product_detection`의 `FAILED`는 해당 제품 처리 결과가 실패했음을 의미
- 장비 오류의 실제 원인과 상태는 `component/status/update`에서 전달받도록 분리
- 서버는 제품 분류 결과만 보고 카메라/센서/서보 오류 원인을 추측하지 않음
- 장비 오류 원인은 `componentCode`, `status`, `eventCode`를 기준으로 처리

예:

```text
제품 분류 결과
→ classificationStatus = FAILED

장비 상태
→ CAMERA / ERROR / CAMERA_ERROR
```

두 메시지는 같은 상황에서 각각 별도로 전송될 수 있다.

### NO_DETECTION 처리 방향

- 제어부 규격에서 `NO_DETECTION`은 `CAMERA / WARNING`으로 정의
- 제품 관점에서는 YOLO 객체 미검출로 인해 `classificationStatus = FAILED`
- 장비 관점에서는 카메라/YOLO 자체의 완전한 고장으로 보기 어려워 `WARNING` 처리 가능
- 현재 서버의 `ProductDetectionService`는 모든 `FAILED`를 ERROR로 처리하고 있어 추후 수정 필요
- 향후 장비 상태 판단은 `component/status/update`를 기준으로 처리하도록 변경 예정

### 구성요소 코드 통일

- 기존 서버 코드에서 사용하던 `VISION_MODULE` 명칭을 `CAMERA`로 통일하기로 결정
- 제어부 규격의 `CAMERA = Picamera2 및 YOLO` 정의에 맞춰 서버 DB 및 관련 코드를 수정 예정

---

## 2026-08-25

### 시스템 구성요소 CAMERA 기준 통일

- 기존 서버와 DB에서 사용하던 `VISION_MODULE`을 `CAMERA`로 통일
- 카메라 촬영과 YOLO 제품 분류 기능을 하나의 `CAMERA` 구성요소로 관리하도록 정리
- 제품 분류 MQTT Topic을 `smart_sorting/vision/product_detection`에서 `smart_sorting/camera/product_detection`으로 변경
- `system_components` 초기 데이터를 실제 시스템 구성 기준으로 다시 정리
- `VISION_MODULE` 제거 후 다음 13개 Component를 기준으로 구성
  - `RASPBERRY_PI`
  - `ARDUINO`
  - `IR_SENSOR`
  - `CAMERA`
  - `CONVEYOR`
  - `SORTING_SERVO`
  - `BUZZER`
  - `WORKER_DISPLAY`
  - `WORKER_UI`
  - `ADMIN_WEB`
  - `MQTT_BROKER`
  - `API_SERVER`
  - `MYSQL_DATABASE`
- 장비 연결 전 초기 상태를 모두 `OFFLINE`으로 재설정

### 실제 장비 상태 MQTT 수신 구현

- 제어부에서 장비 상태를 서버로 전달하기 위한 `smart_sorting/component/status/update` Topic Subscribe 구현
- 실제 장비 상태 수신용 `ComponentStatusUpdateRequest` DTO 추가
- Payload 필드를 다음과 같이 구성
  - `ComponentCode`
  - `Status`
  - `ErrorCode`
  - `Message`
- MQTT 메시지 역직렬화 시 대소문자 차이와 관계없이 필드를 읽을 수 있도록 처리
- `componentCode`와 `status`는 서버 처리 전에 대문자로 통일
- 허용 상태값을 다음 네 가지로 제한
  - `NORMAL`
  - `WARNING`
  - `ERROR`
  - `OFFLINE`
- 수신한 `componentCode`를 기준으로 `system_components`를 조회하고 `current_status`, `status_updated_at` 갱신
- 실제 상태가 변경된 경우에만 `[COMPONENT]` 로그를 출력하도록 구성

```text
제어부
        ↓
smart_sorting/component/status/update
        ↓
MqttSubscriberService
        ↓
system_components 조회
        ↓
current_status 갱신
```

### Component 상태 MQTT 동작 테스트

- MQTT Explorer를 이용하여 `CAMERA` 상태 변경 테스트
- `CAMERA = NORMAL` 메시지 수신 후 DB 상태가 `NORMAL`로 변경되는 것을 확인
- `CAMERA = ERROR` 메시지 수신 후 DB 상태가 `ERROR`로 변경되는 것을 확인
- 상태 변경 시 `[MQTT]`, `[COMPONENT]` 로그가 정상 출력되는 것을 확인

예시 Payload:

```json
{
  "componentCode": "CAMERA",
  "status": "ERROR",
  "errorCode": "CAMERA_ERROR",
  "message": "카메라 촬영에 실패했습니다."
}
```

### 제품 분류 실패와 장비 상태 처리 분리

- 기존 `ProductDetectionService`에서 제품 분류 `FAILED` 시 자동으로 `CAMERA = ERROR` 처리하던 로직 제거
- 제품 분류 실패만으로 장비 오류를 판단하지 않도록 구조 수정
- 제품 분류 결과와 실제 Component 상태의 역할을 분리
  - `smart_sorting/camera/product_detection` → 제품 한 건의 분류 결과
  - `smart_sorting/component/status/update` → 실제 장비 및 Component 상태
- 제품 분류 `FAILED` 시 자동 `ERROR` Alert 생성 처리 제거
- 제품 분류 실패 시 `CAMERA` 상태를 자동 변경하지 않도록 수정

```text
제품 분류 결과
        ↓
SUCCESS / FAILED
        ↓
product_detections 저장

장비 상태
        ↓
NORMAL / WARNING / ERROR / OFFLINE
        ↓
system_components 갱신
```

- `FAILED`는 해당 제품의 분류 결과 실패를 의미하며 장비 고장을 의미하지 않도록 기준 정리
- Confidence 부족과 같이 제품 분류에는 실패하지만 장비는 정상일 수 있는 상황을 별도로 처리할 수 있도록 구조 분리

### 제품 분류 FAILED 처리 테스트

- 기존 실행 파일이 남아 있어 수정 전 동작이 나타나는 문제 확인
- 서버 Clean / Build / 재실행 후 수정된 로직으로 다시 테스트
- MQTT 제품 분류 `FAILED` 메시지 수신 확인
- `product_detections`에 `FAILED` 결과가 정상 저장되는 것을 확인
- `[DETECTION] 제품 분류 실패` 로그 출력 확인
- `CAMERA` 상태가 자동으로 `ERROR`로 변경되지 않는 것을 확인
- 제품 분류 실패에 따른 Alert가 자동 생성되지 않는 것을 확인
- `smart_sorting/alert` MQTT Publish가 발생하지 않는 것을 확인

```text
product_detection FAILED
        ↓
product_detections FAILED 저장
        ↓
[DETECTION] 제품 분류 실패
        ↓
장비 상태 변경 없음
Alert 생성 없음
```

### 제품 분류 및 Component 오류 처리 기준 정리

- 제어부와 서버에서 동일한 기준으로 제품 분류 결과와 장비 상태를 처리할 수 있도록 규격을 재정리
- 제품 분류 기본 기준을 다음과 같이 정의
  - Confidence `> 0.70` → `SUCCESS`
  - Confidence `<= 0.70` → `FAILED`, `productTypeCode = null`
  - Class 0 → `CANDY`
  - Class 1 → `CHOCOLATE`
- 제품 분류 실패와 Component 상태가 함께 발생하는 상황을 구분
  - YOLO 객체 미검출
  - YOLO Inference 오류
  - YOLO 모델 로딩 오류
  - Camera 촬영 실패
  - 이미지 저장 실패
  - Arduino Serial 연결 끊김
  - Arduino Serial 통신 오류
  - Serial 응답 Timeout
  - IR Sensor 오류
  - Stepper Motor 오류
- Servo ACK Timeout 및 Servo ACK 불일치는 제품 인식 결과를 유지하면서 `SORTING_SERVO` 상태만 `ERROR`로 처리하도록 기준 정리
- Buzzer 오류, Arduino 동작 오류, 알 수 없는 Arduino 명령은 제품 분류 결과와 직접 연결하지 않고 Component 상태만 처리하도록 분리
- Component 정상 상태는 Alert 생성 대상이 아니라 현재 상태 동기화 용도로 사용하도록 정리
  - `status = NORMAL`
  - `errorCode = null`
  - `priority = null`

### Error Code 및 Alert Priority 기준 정리

- 제어부와 서버가 공통으로 사용할 Error Code를 Hardware / Software 기준으로 구분
- Hardware Error Code 정리
  - `CAMERA_ERROR`
  - `SERVO_ACK_TIMEOUT`
  - `SERVO_ACK_ERROR`
  - `STEPPER_ERROR`
  - `IR_ERROR`
  - `BUZZER_ERROR`
  - `ARDUINO_ERROR`
- Software Error Code 정리
  - `YOLO_ERROR`
  - `MODEL_LOAD_ERROR`
  - `NO_DETECTION`
  - `IMAGE_SAVE_ERROR`
  - `SERIAL_DISCONNECTED`
  - `SERIAL_ERROR`
  - `SERIAL_TIMEOUT`
  - `UNKNOWN_COMMAND`
- Alert Priority 기준을 다음과 같이 정리
  - `LOW` : 정상 처리 결과 및 단순 안내
  - `MEDIUM` : 생산에 영향을 줄 수 있어 확인이 필요한 상태
  - `HIGH` : 장비 정지, 연결 끊김 등 즉시 확인 및 조치가 필요한 상태
- `NO_DETECTION`은 `CAMERA / WARNING / MEDIUM`으로 처리
- 주요 장비 `ERROR`, `OFFLINE` 상태는 `HIGH` 우선순위를 사용하는 방향으로 정리

### 제어부-서버 인터페이스 문서 작성

- 제어부와 서버가 함께 참고할 수 있도록 `CONTROL_INTERFACE_SPEC.md` 작성
- 제품 분류 결과 예시와 처리 기준 정리
- Component 정상 및 오류 상태를 `componentCode`, `status`, `errorCode`, `message`, `priority` 기준으로 정리
- 제품 분류 결과와 Component 상태가 동시에 발생하는 경우의 처리 기준 정리
- Hardware / Software Error Code 정리
- 실제 DB에서 사용하는 Component Code, Component Status, Alert Priority 정리
- 제어부 → 서버 MQTT Topic 및 Payload 형식 정리

```text
smart_sorting/camera/product_detection
smart_sorting/component/status/update
```

- `MQTT_BROKER`, `API_SERVER`, `MYSQL_DATABASE`와 같은 서버 공통 인프라 자체의 오류 처리 기준은 제어부용 처리 기준에서 제외
- 실제 DB에 존재하는 Component이므로 `CONTROL_INTERFACE_SPEC.md`의 Component Code 목록에는 유지

---

## 2026-08-26

### 생산 목표 단위 세트 기준 통일

- 생산 목표 단위를 초콜릿과 사탕 모두 **세트 수 기준**으로 통일
- 기존 사탕 목표 컬럼 및 필드의 `Count` 의미를 세트 기준으로 명확하게 변경
  - `target_candy_count` → `target_candy_set_count`
  - `TargetCandyCount` → `TargetCandySetCount`
- `production_targets`, `production_sessions`에서 동일한 명칭을 사용하도록 정리
- 초콜릿과 사탕의 실제 생산량은 기존과 동일하게 낱개 수로 저장
  - `chocolate_count`
  - `candy_count`
- 목표 세트 수와 실제 생산 개수를 분리하여 관리하도록 기준 정리

```text
생산 목표
        ↓
세트 단위로 저장

target_chocolate_set_count
target_candy_set_count

실제 생산량
        ↓
낱개 단위로 저장

chocolate_count
candy_count
```

### 데이터베이스 스키마 및 Entity 매핑 수정

- `production_targets`의 사탕 목표 컬럼을 `target_candy_set_count`로 변경
- `production_sessions`의 사탕 목표 컬럼을 `target_candy_set_count`로 변경
- 변경된 스키마를 기준으로 데이터베이스를 다시 생성
- `ProductionTarget`, `ProductionSession` Entity의 속성명을 변경된 컬럼 기준으로 수정
- `AppDbContext` Fluent API 매핑을 변경된 컬럼명에 맞춰 수정

```text
ProductionTarget.TargetCandySetCount
        ↓
production_targets.target_candy_set_count

ProductionSession.TargetCandySetCount
        ↓
production_sessions.target_candy_set_count
```

### unit_per_set 기반 목표 낱개 수 계산 적용

- 목표 생산량을 DB에 낱개 수로 중복 저장하지 않고 `product_types.unit_per_set`을 이용하여 계산하도록 정리
- 초콜릿과 사탕 모두 동일한 계산 방식을 사용하도록 수정

```text
목표 낱개 수
= 목표 세트 수 × unit_per_set
```

예시:

```text
CHOCOLATE
15세트 × 10개 = 150개

CANDY
50세트 × 1개 = 50개
```

- `ProductionSessionsController`의 생산 시작 및 종료 처리에 해당 계산 방식 적용
- `ProductDetectionService`의 생산 현황 계산에도 동일한 기준 적용
- 제품별 세트 수는 실제 생산 개수를 `unit_per_set`으로 나누어 계산하도록 유지

```text
현재 세트 수
= 현재 생산 개수 / unit_per_set
```

### 생산 목표 REST API 필드 정리

- 생산 목표 조회 및 변경 API에서 사탕 목표 필드를 세트 기준으로 변경
- 요청과 응답에서 초콜릿과 사탕의 목표 필드 형식을 통일

```text
targetChocolateSetCount
targetCandySetCount
```

사용 API:

```text
GET /api/production-targets/current
PUT /api/production-targets/current
```

예시 요청:

```json
{
  "targetChocolateSetCount": 15,
  "targetCandySetCount": 50
}
```

- 생산 세션 시작 및 현재 세션 조회, 종료 응답에서도 `targetCandySetCount`를 사용하도록 수정

### 생산 현황 MQTT 계산 기준 수정

- `smart_sorting/production/status`의 `targetCount`를 목표 세트 수와 `unit_per_set`을 이용하여 계산하도록 수정
- `currentCount`는 실제 생산된 낱개 수를 유지
- `setCount`는 실제 생산량을 제품별 `unit_per_set`으로 나눈 값으로 계산
- `progress`는 실제 생산 낱개 수와 목표 낱개 수를 기준으로 계산

```text
목표 세트 수
        ↓
× unit_per_set
        ↓
targetCount

현재 생산 개수
        ↓
currentCount

현재 생산 개수 / unit_per_set
        ↓
setCount

currentCount / targetCount × 100
        ↓
progress
```

- 생산 시작, 제품 감지, 생산 종료 시 동일한 계산 기준으로 MQTT Payload가 생성되도록 정리

### 생산 목표 및 세션 동작 테스트

- 생산 목표 조회 API 테스트
- 생산 목표 변경 API 테스트
- 변경한 목표가 DB에 정상 저장되는 것을 확인
- 생산 세션 생성 시 목표 세트 수가 세션에 정상 저장되는 것을 확인
- 생산 시작 MQTT 메시지의 목표 낱개 수 계산 확인

테스트 예시:

```text
초콜릿 목표 = 15세트
초콜릿 unit_per_set = 10
→ targetCount = 150

사탕 목표 = 50세트
사탕 unit_per_set = 1
→ targetCount = 50
```

### 제품 감지 및 생산량 계산 테스트

- `CHOCOLATE / SUCCESS` 제품 감지 메시지 수신 후 `chocolate_count` 증가 확인
- `CANDY / SUCCESS` 제품 감지 메시지 수신 후 `candy_count` 증가 확인
- 제품 감지 후 MQTT 생산 현황의 `currentCount`, `setCount`, `progress` 변경 확인
- `unit_per_set` 값이 계산에 실제로 적용되는지 확인하기 위해 사탕의 `unit_per_set`을 임시로 `5`로 변경하여 테스트
- 사탕 목표 낱개 수와 현재 세트 수가 변경된 `unit_per_set` 기준으로 계산되는 것을 확인
- 테스트 후 사탕의 `unit_per_set`을 기존 값 `1`로 복원

### 생산 세션 종료 테스트

- 목표 수량을 달성하지 않은 상태에서 생산 세션 종료 테스트
- 미달성 상태에서 세션이 `CANCELLED`로 변경되는 것을 확인
- 종료 시 MQTT 생산 현황과 DB 상태가 함께 변경되는 것을 확인

- 작은 목표값을 설정한 뒤 목표 수량만큼 제품 감지 데이터를 입력하여 완료 테스트
- 초콜릿과 사탕 목표를 모두 달성한 상태에서 생산 세션 종료
- 세션 상태가 `COMPLETED`로 변경되는 것을 확인
- 제품별 진행률이 `100%`로 계산되는 것을 확인
- 종료 MQTT 메시지와 DB 저장 결과가 동일한 기준으로 반영되는 것을 확인

---

## 2026-08-27 — 관리자 대시보드 조회 개선 및 생산 목표·작업 인원 예약 기능 확장

### 관리자 대시보드 최근 제품 감지 조회 개선

- 관리자 대시보드의 최근 제품 분류 이미지가 오늘 날짜의 감지 결과만 조회하도록 되어 있어, 오늘 데이터가 없으면 이전 날짜의 최근 이미지가 표시되지 않는 문제를 확인했습니다.
- `GET /api/dashboard/recent-detections`의 오늘 날짜 조건을 제거하고 전체 `product_detections`를 대상으로 최신순 정렬하도록 수정했습니다.
- 대시보드에서는 가장 최근 결과 5개만 표시하도록 조회 개수를 조정했습니다.

```text
기존
오늘 product_detections
        ↓
최신순
        ↓
최대 10개

변경
전체 product_detections
        ↓
detected_at DESC
        ↓
최신 5개
```

### 관리자 대시보드 제품 분류 비율 기준 수정

- 기존 `GET /api/dashboard/classification-ratio`는 `SUCCESS / FAILED` 비율을 계산하고 있었습니다.
- 관리자 대시보드 화면의 도넛 차트가 `초콜릿 / 사탕` 구성 비율을 표시하도록 설계되어 있어 API 계산 기준을 수정했습니다.
- 오늘 정상 분류된 `SUCCESS` 데이터만 대상으로 `CHOCOLATE`, `CANDY` 개수와 비율을 계산하도록 변경했습니다.
- 제품 종류를 확정할 수 없는 `FAILED` 결과는 제품 종류 비율 계산에서 제외하도록 정리했습니다.

```text
기존
SUCCESS / FAILED 비율

변경
CHOCOLATE / CANDY 비율
```

응답 예시:

```json
{
  "totalCount": 100,
  "chocolateCount": 60,
  "candyCount": 40,
  "chocolateRate": 60.0,
  "candyRate": 40.0
}
```

### 생산 목표와 작업 인원 설정 역할 분리

- 관리자 화면에서 생산 목표 설정과 작업 인원 설정을 별도 기능으로 제공하는 구조에 맞춰 API 역할을 분리했습니다.

```text
GET /api/production-targets/current
→ 현재/예약 생산 목표 및 현재/예약 작업 인원 조회

PUT /api/production-targets/current
→ 생산 목표만 설정 또는 예약

PUT /api/production-targets/worker-count
→ 작업 인원만 설정 또는 예약
```

- `PUT /api/production-targets/current` 요청은 생산 목표 필드만 받도록 유지했습니다.
- 생산 목표 변경 API 응답에서도 작업 인원 필드를 제거하여 역할을 명확히 분리했습니다.
- `GET /api/production-targets/current`는 관리자 화면 최초 로딩용 조회 API이므로 현재값과 예약값을 모두 반환하도록 유지했습니다.

### 작업 인원 설정 API 구현

- 하루 작업 인원을 별도로 변경할 수 있도록 다음 API를 구현했습니다.

```text
PUT /api/production-targets/worker-count
```

요청 예시:

```json
{
  "dailyWorkerCount": 4
}
```

- 작업 인원은 1명 이상이어야 하도록 검증했습니다.
- 적용 대상 생산 목표보다 작업 인원이 많아지는 경우 요청을 차단하도록 검증했습니다.
- 작업 인원 값은 생산 세션 생성 시 하루 생산 목표를 작업자 수 기준으로 분배하는 값으로 사용하도록 기존 세션 분배 로직과 연결했습니다.

### 예약 작업 인원 데이터베이스 구조 추가

- 생산 목표와 동일하게 작업 인원도 다음 날 적용할 값을 예약할 수 있도록 `production_targets`에 예약 작업 인원 컬럼을 추가했습니다.

```text
daily_worker_count
→ 현재 적용 중인 하루 작업 인원

next_daily_worker_count
→ 다음 날 적용할 예약 작업 인원
```

추가 컬럼:

```sql
next_daily_worker_count INT NULL
```

- 예약 작업 인원은 값이 없을 경우 `NULL`, 값이 존재하는 경우 1 이상만 저장되도록 `CHECK` 제약조건을 추가했습니다.
- 초기 데이터는 현재 작업 인원 `3`, 예약 작업 인원 `NULL`로 설정했습니다.
- DB 재생성 스크립트와 확인용 `SELECT`에도 예약 작업 인원 컬럼을 반영했습니다.

### ProductionTarget Entity 및 EF Core 매핑 수정

- `ProductionTarget` Entity에 예약 작업 인원 속성을 추가했습니다.

```csharp
public int? NextDailyWorkerCount { get; set; }
```

- `AppDbContext` Fluent API에 다음 매핑을 추가했습니다.

```text
ProductionTarget.NextDailyWorkerCount
        ↓
production_targets.next_daily_worker_count
```

### 작업 인원 즉시 적용 및 다음 날 예약 처리

- 작업 인원 설정도 생산 목표와 동일한 적용 규칙을 사용하도록 수정했습니다.

```text
오늘 생산 세션 없음
→ daily_worker_count 바로 변경

오늘 생산 세션 있음
→ daily_worker_count 유지
→ next_daily_worker_count에 다음 날 값 예약
```

예:

```text
현재 작업 인원 = 4명

오늘 생산 시작 후 5명으로 변경
        ↓
daily_worker_count = 4
next_daily_worker_count = 5
```

### 생산 목표 예약과 작업 인원 예약의 상호 검증

- 생산 목표와 작업 인원을 각각 따로 예약할 수 있기 때문에 서로 다른 순서로 값을 변경해도 잘못된 조합이 저장되지 않도록 검증을 보완했습니다.

생산 목표 예약 시:

```text
next_daily_worker_count 존재
→ 예약 작업 인원 기준으로 목표 최소값 검사

next_daily_worker_count 없음
→ 현재 daily_worker_count 기준으로 검사
```

작업 인원 예약 시:

```text
next_target_* 존재
→ 예약 생산 목표 기준으로 작업 인원 검사

next_target_* 없음
→ 현재 생산 목표 기준으로 검사
```

- 예를 들어 현재 인원 3명, 예약 인원 5명인 상태에서 다음 생산 목표를 `4 / 4`로 예약하려는 경우 요청을 차단하도록 수정했습니다.

### 예약값 자동 적용 로직 확장

- 기존에는 다음 날 예약 생산 목표만 현재 목표로 전환했지만, 예약 작업 인원도 함께 적용되도록 수정했습니다.
- `GET /api/production-targets/current`에서 오늘 생산 세션이 없는 경우 예약 생산 목표와 예약 작업 인원을 각각 확인하도록 변경했습니다.
- 생산 목표 예약과 작업 인원 예약은 서로 독립적으로 존재할 수 있으므로 각각 별도로 적용하도록 구현했습니다.
- 실제 변경된 예약값이 있을 때만 `updated_at` 갱신 및 DB 저장을 수행하도록 정리했습니다.

```text
오늘 세션 없음
        ↓
예약 생산 목표 적용
        ↓
예약 작업 인원 적용
        ↓
적용된 next 값 NULL 처리
```

### 생산 세션 시작 시 예약값 적용

- 관리자 웹을 열지 않고 작업자가 바로 생산을 시작하는 경우에도 예약값이 적용되어야 하므로 `ProductionSessionsController`의 생산 시작 로직을 수정했습니다.
- 오늘 첫 생산 세션인 경우 예약 생산 목표와 예약 작업 인원을 모두 확인하도록 변경했습니다.
- 예약값이 있으면 현재 설정으로 반영한 뒤 `next_*` 값을 `NULL`로 초기화하도록 구현했습니다.

```text
POST /api/production-sessions/start
        ↓
오늘 첫 세션인지 확인
        ↓
예약 목표/작업 인원 적용
        ↓
현재 목표와 작업 인원 기준 세션 목표 분배
```

- 다음 두 경로 모두에서 다음 날 예약값을 적용할 수 있도록 구성했습니다.

```text
GET /api/production-targets/current

또는

POST /api/production-sessions/start
```

### 관리자 웹 표시 기준 정리

- 관리자 설정 화면에서는 예약값이 존재하면 예약값을 우선 표시하고, 예약값이 없으면 현재값을 표시하도록 기준을 정리했습니다.

```javascript
const chocolateTarget =
    data.nextTargetChocolateSetCount
    ?? data.targetChocolateSetCount;

const candyTarget =
    data.nextTargetCandySetCount
    ?? data.targetCandySetCount;

const workerCount =
    data.nextDailyWorkerCount
    ?? data.dailyWorkerCount;
```

```text
예약값 있음 → 예약값 표시
예약값 없음 → 현재값 표시
```

### 예약 기능 동작 테스트

- 오늘 생산 세션이 없는 상태에서 생산 목표와 작업 인원을 변경하여 현재값으로 즉시 반영되는 것을 확인했습니다.
- 작업 인원 변경 후 `GET /api/production-targets/current`에서 변경된 `dailyWorkerCount`가 조회되는 것을 확인했습니다.
- 변경된 작업 인원 기준으로 생산 세션 목표가 정상 분배되는 것을 확인했습니다.
- 설정된 작업 인원 수만큼 세션 생성 후 추가 세션 생성이 차단되는 것을 확인했습니다.
- 오늘 생산이 시작된 상태에서 생산 목표를 변경하면 현재 목표는 유지되고 `next_target_*`에 예약되는 것을 확인했습니다.
- 오늘 생산이 시작된 상태에서 작업 인원을 변경하면 현재 인원은 유지되고 `next_daily_worker_count`에 예약되는 것을 확인했습니다.

### 서버 재시작 후 예약값 유지 테스트

- 생산 목표와 작업 인원 예약값을 저장한 뒤 ASP.NET Core 서버를 종료하고 다시 실행했습니다.
- 서버 재시작 후에도 다음 예약값이 MySQL에 유지되는 것을 확인했습니다.

```text
next_target_chocolate_set_count
next_target_candy_set_count
next_daily_worker_count
```

- 예약값을 서버 메모리가 아닌 DB에 저장하므로 서버 재시작에 영향을 받지 않는 것을 확인했습니다.

### 다음 날 예약값 적용 시뮬레이션 테스트

- 실제 날짜 변경을 기다리지 않고 오늘 생성된 테스트 세션의 `started_at`을 이전 날짜로 변경하여 다음 날 상황을 시뮬레이션했습니다.
- 오늘 세션이 없는 상태에서 `GET /api/production-targets/current`를 호출해 예약값이 현재값으로 정상 적용되는 것을 확인했습니다.
- 적용 후 예약 생산 목표 및 예약 작업 인원 필드가 모두 `NULL`로 초기화되는 것을 확인했습니다.

```text
적용 전

현재 목표 = 120 / 60
예약 목표 = 150 / 80
현재 인원 = 4
예약 인원 = 5

적용 후

현재 목표 = 150 / 80
예약 목표 = NULL / NULL
현재 인원 = 5
예약 인원 = NULL
```

### 관리자 웹 연동 문서 정리

- 관리자 웹 담당자가 생산 목표와 작업 인원 설정 기능을 연동할 수 있도록 통합 연동 가이드를 작성했습니다.
- 현재/예약 생산 목표, 현재/예약 작업 인원, 즉시 적용/예약 처리, 서버 재시작 후 유지, 다음 날 자동 적용, 화면 표시 기준, JavaScript 요청 예시를 문서화했습니다.

---

## 2026-08-28 — MQTT 실시간 연동 및 알림 처리 구조 개선

### 제품 감지 결과 실시간 MQTT 연동

- 관리자 Web의 최근 제품 분류 내역을 실시간으로 갱신할 수 있도록 신규 MQTT Topic을 추가했습니다.
  - `smart_sorting/product/detection`
- 제품 감지 결과가 DB에 저장된 직후 신규 감지 정보를 MQTT로 Publish하도록 `ProductDetectionService`를 수정했습니다.
- 분류 성공 여부와 관계없이 모든 감지 결과를 전달하도록 구성했습니다.
  - `SUCCESS`
    - 제품 유형
    - 신뢰도
    - 이미지 경로
    - 감지 시간
  - `FAILED`
    - 제품 유형 `null`
    - 신뢰도 `null`
    - 이미지 경로
    - 감지 시간
- 기존 `smart_sorting/production/status`와 역할을 분리했습니다.
  - `production/status`: 생산 수량, 세트 수, 진행률
  - `product/detection`: 최근 제품 감지 결과
- MQTT Explorer를 이용하여 `CHOCOLATE`, `CANDY`, `FAILED` 감지 결과가 정상 Publish되는 것을 확인했습니다.

### 알림 MQTT Payload 구조 개선

- Qt 작업자 화면과 관리자 Web에서 서로 다른 길이의 알림 메시지를 사용할 수 있도록 알림 메시지를 분리했습니다.
  - `shortMessage`: Qt 작업자 화면용 짧은 메시지
  - `alertMessage`: 관리자 Web 및 DB용 상세 메시지
- 자동 장비 알림 MQTT Payload를 다음 구조로 통일했습니다.
  - `alertId`
  - `alertType`
  - `priority`
  - `componentCode`
  - `errorCode`
  - `shortMessage`
  - `alertMessage`
  - `createdAt`
- 관리자 수동 알림에도 동일한 Payload 구조를 적용했습니다.
- 수동 알림 생성 요청 시 `shortMessage`를 필수 입력으로 추가하고 50자 이하로 제한했습니다.
- INFO 알림에도 동일한 메시지 구조를 적용했습니다.

### Alert 오류 코드 구조 추가

- 장비에서 발생한 오류의 종류를 구분할 수 있도록 `alerts` 테이블에 `error_code` 컬럼을 추가했습니다.
- `Alert` Entity와 `AppDbContext`에 `ErrorCode` 매핑을 추가했습니다.
- 알림 조회 API에서도 `errorCode`를 반환하도록 수정했습니다.
- 자동 장비 오류는 `errorCode`를 저장하고, 수동 알림과 INFO 알림은 `NULL`로 처리하도록 역할을 구분했습니다.

### 장비 오류 알림 처리 구조 분리

- 장비 상태 이벤트와 알림 생성 로직을 분리하기 위해 `ComponentAlertService`를 추가했습니다.
- Error Code에 따라 대상 구성요소, 장비 상태, 짧은 메시지, 상세 메시지, 중요도를 서버에서 결정하도록 구성했습니다.
- 주요 오류 코드를 정리했습니다.
  - `NO_DETECTION`
  - `CAMERA_ERROR`
  - `YOLO_ERROR`
  - `MODEL_LOAD_ERROR`
  - `IMAGE_SAVE_ERROR`
  - `SERVO_ACK_TIMEOUT`
  - `SERVO_ACK_ERROR`
  - `STEPPER_ERROR`
  - `IR_ERROR`
  - `BUZZER_ERROR`
  - `SERIAL_DISCONNECTED`
  - `SERIAL_ERROR`
  - `SERIAL_TIMEOUT`
  - `ARDUINO_ERROR`
  - `UNKNOWN_COMMAND`
- 동일 구성요소에서 동일한 Error Code가 복구되지 않은 상태로 존재하면 중복 Alert를 생성하지 않도록 처리했습니다.
- 장비가 `NORMAL` 상태로 복구되면 해당 구성요소의 자동 알림을 `RECOVERED` 상태로 변경하도록 구성했습니다.
- 수동으로 생성한 알림은 자동 복구 대상에서 제외했습니다.

### 장비 상태와 알림 Topic 역할 분리

- 신규 오류 발생과 현재 장비 상태의 역할을 다음과 같이 분리했습니다.
  - `smart_sorting/alert`: 신규 INFO / WARNING / ERROR 알림 전달
  - `smart_sorting/component/status`: 장비 현재 상태 전달
- 장비 상태를 `NORMAL`, `WARNING`, `ERROR`, `OFFLINE`으로 통일했습니다.
- 오류 복구 시 별도의 복구 Alert를 생성하지 않고 `component/status`의 `NORMAL` 메시지로 정상 복귀를 전달하도록 결정했습니다.
- 관리자 Web의 과거 알림 및 복구 이력은 REST API와 DB에서 조회하도록 역할을 분리했습니다.

### 초콜릿 세트 완료 INFO 알림 개선

- 기존에는 현재 생산 세션의 `ChocolateCount`를 기준으로 초콜릿 세트 완료 여부를 판단했습니다.
- 작업자 교대 시 새로운 생산 세션의 생산량이 0부터 시작하기 때문에 이전 작업자가 생산한 미완성 세트가 다음 작업자에게 이어지지 않는 문제가 있었습니다.
- 작업자별 생산 실적과 실제 공정의 세트 완료 기준을 분리했습니다.
  - `production_sessions.chocolate_count`: 현재 작업자 개인 생산 실적
  - Qt 생산 현황: 현재 작업자 세션 기준
  - 초콜릿 세트 완료 INFO: 당일 전체 생산 세션의 초콜릿 누적 생산량 기준
- 당일 전체 `ChocolateCount`를 합산한 뒤 이번 감지 수량을 포함하여 세트 완료 여부를 계산하도록 수정했습니다.
- 작업자가 세트 단위 생산을 완료하지 못해도 작업을 종료할 수 있도록 기존 작업 종료 방식을 유지했습니다.

### 작업자 교대 및 일일 누적 INFO 테스트

- 기존 당일 생산 기록을 유지한 상태에서 작업자 교대 상황을 테스트했습니다.
- 테스트 시작 당시 당일 초콜릿 누적 생산량은 111개였습니다.
- 기존 생산 세션에서 초콜릿 6개를 추가 생산하여 누적 117개까지 증가시켰습니다.
- 기존 세션을 종료한 뒤 새로운 생산 세션을 생성했습니다.
- 새로운 세션의 `chocolate_count`가 0부터 시작하는 것을 확인했습니다.
- 신규 세션에서 초콜릿 3개를 추가 생산하여 당일 누적 생산량을 120개로 만들었습니다.
- 118개와 119개에서는 INFO 알림이 발생하지 않았고, 120개가 되는 순간 다음 알림이 발생하는 것을 확인했습니다.

```text
shortMessage = 초콜릿 12세트 완료
alertMessage = 초콜릿 12세트 생산이 완료되었습니다.
```

- 신규 세션의 DB 생산 실적은 3개로 별도 저장되는 것을 확인했습니다.
- 최종적으로 다음 구조가 정상 동작하는 것을 확인했습니다.

```text
작업자 개인 생산 실적
→ production_sessions 단위로 별도 저장

Qt 생산 현황
→ 현재 작업자 세션 기준

초콜릿 세트 완료 INFO
→ 당일 전체 작업자 누적 생산량 기준
```

### MQTT 연동 문서 정리

- Qt 작업자 화면 알림 MQTT 연동 기준을 정리했습니다.
  - `smart_sorting/alert`
  - `smart_sorting/component/status`
  - `shortMessage` 사용
  - 장비 `NORMAL` 복구 처리

- 관리자 Web의 최근 제품 분류 실시간 표시를 위한 MQTT 연동 기준을 정리했습니다.
  - `smart_sorting/product/detection`
  - REST + MQTT 병행
  - SUCCESS / FAILED 실시간 반영

- 관리자 Web의 알림 MQTT 변경사항도 별도로 정리했습니다.
  - `alertMessage` 중심 표시
  - `errorCode`
  - `component/status`
  - INFO 알림
  - 수동 알림 생성 시 `shortMessage`, `alertMessage` 전달

---

## 2026-09-02 — 관리자 Web 제품 감지 내역 및 알림/이상 내역 API 확장

### 제품 감지 내역 조회 API 구현

- 관리자 Web의 제품 이미지 화면에서 전체 제품 감지 이력을 조회할 수 있도록 제품 감지 목록 API를 구현했습니다.
- `GET /api/product-detections`에서 페이지네이션을 지원하도록 구성했습니다.
  - `page` 기본값: `1`
  - `pageSize` 기본값: `15`
  - 최대 `pageSize`: `100`
- 최신 감지 결과부터 표시할 수 있도록 `product_detection_id` 내림차순으로 조회하도록 구성했습니다.
- 목록 응답에 다음 페이지 정보를 포함하도록 구성했습니다.
  - `page`
  - `pageSize`
  - `totalCount`
  - `totalPages`

### 제품 감지 내역 필터 및 검색 기능 구현

- 관리자 Web의 제품 이미지 화면 필터와 연동할 수 있도록 제품 유형 및 분류 상태 필터를 추가했습니다.
- 제품 유형 필터를 다음 값으로 제한했습니다.
  - `CHOCOLATE`
  - `CANDY`
- 분류 실패 조회를 위해 `status=FAILED` 필터를 추가했습니다.
- 허용되지 않은 제품 유형 또는 분류 상태가 전달되면 `400 Bad Request`를 반환하도록 검증 로직을 추가했습니다.
- 감지 ID를 이용한 정확 일치 검색 기능을 구현했습니다.
- 숫자가 아닌 감지 ID 검색값이 전달되면 `400 Bad Request`를 반환하도록 처리했습니다.
- 제품 유형, 분류 상태, 감지 ID 검색을 함께 사용할 수 있도록 구성했습니다.

### 제품 감지 상세 조회 API 구현

- 관리자 Web에서 특정 감지 결과의 상세 정보를 조회할 수 있도록 상세 조회 API를 구현했습니다.
  - `GET /api/product-detections/{productDetectionId}`
- 제품 감지 상세 응답에 다음 정보를 포함하도록 구성했습니다.
  - 제품 감지 ID
  - 생산 작업 ID
  - 제품 유형 코드
  - 신뢰도
  - 이미지 경로
  - 분류 상태
  - 감지 시각
- 존재하지 않는 감지 ID를 조회할 경우 `404 Not Found`를 반환하도록 처리했습니다.
- 관리자 Web에서는 `product_detection_id`를 별도의 표시용 ID로 가공하지 않고 DB의 실제 ID 값을 그대로 사용하도록 결정했습니다.

### 제품 감지 API 시간 형식 정리

- 관리자 Web에서 시간대를 명확하게 처리할 수 있도록 제품 감지 REST API의 `detectedAt` 형식을 정리했습니다.
- DB에 저장된 KST 기준 시각에 `+09:00` 오프셋을 명시하여 ISO 8601 형식으로 반환하도록 구성했습니다.

```text
2026-09-02T14:30:00+09:00
```

- 목록 조회와 상세 조회에서 동일한 시간 형식을 사용하도록 통일했습니다.
- `productTypeCode`, `confidence`, `imagePath`는 분류 실패 등의 경우 `null`이 될 수 있도록 기존 DB 구조를 유지했습니다.

### 알림 목록 조회 API 확장

- 관리자 Web의 알림/이상 내역 화면과 연동할 수 있도록 기존 알림 조회 API를 확장했습니다.
- `GET /api/alerts`에 페이지네이션을 적용했습니다.
  - `page` 기본값: `1`
  - `pageSize` 기본값: `10`
  - 최대 `pageSize`: `100`
- 최신 알림부터 표시할 수 있도록 `alert_id` 내림차순으로 조회하도록 구성했습니다.
- 목록 응답에 페이지 정보를 포함하도록 구성했습니다.
  - `page`
  - `pageSize`
  - `totalCount`
  - `totalPages`
- 관리자 Web에서 알림 발생 장비를 바로 표시할 수 있도록 목록 응답에 `componentCode`를 추가했습니다.

### 알림 상태 필터 및 검색 기능 구현

- 관리자 Web의 알림 상태 필터와 연동할 수 있도록 `status` Query Parameter를 추가했습니다.
- 하나의 `status` 파라미터에서 다음 상태를 구분하도록 구성했습니다.
  - `UNCHECKED`: 미확인
  - `NOT_RECOVERED`: 미복구
  - `RECOVERED`: 복구 완료
- 전체 조회는 `status`를 전달하지 않는 방식으로 구성했습니다.
- 허용되지 않은 상태값이 전달되면 `400 Bad Request`를 반환하도록 검증 로직을 추가했습니다.
- 장비 코드 또는 알림 내용을 기준으로 검색할 수 있도록 `search` Query Parameter를 추가했습니다.
- 상태 필터와 검색 조건을 동시에 적용할 수 있도록 구성했습니다.

```text
GET /api/alerts?status=NOT_RECOVERED&search=CAMERA
```

### 알림 요약 통계 API 구현

- 관리자 Web 알림/이상 내역 화면 상단의 요약 정보를 제공하기 위해 알림 통계 API를 구현했습니다.
  - `GET /api/alerts/summary`
- 다음 세 가지 통계를 반환하도록 구성했습니다.
  - `todayCount`: 오늘 발생한 전체 알림 수
  - `uncheckedCount`: 날짜와 관계없이 미확인 상태인 알림 수
  - `notRecoveredCount`: 날짜와 관계없이 미복구 상태인 알림 수
- `INFO` 알림은 확인 및 복구 대상이 아니므로 `UNCHECKED`, `NOT_RECOVERED` 집계에 포함되지 않도록 기존 `NULL` 상태 구조를 그대로 활용했습니다.

### 알림 상세 조회 API 구현

- 관리자 Web에서 선택한 알림의 상세 정보를 조회할 수 있도록 상세 조회 API를 구현했습니다.
  - `GET /api/alerts/{alertId}`
- 상세 응답에 다음 정보를 포함하도록 구성했습니다.
  - 알림 ID
  - 생산 작업 ID
  - 제품 감지 ID
  - 확인 사용자 ID
  - 알림 유형
  - 우선순위
  - 오류 코드
  - 복구 상태
  - 확인 상태
  - 알림 내용
  - 시스템 구성요소 코드
  - 발생 시각
  - 복구 시각
  - 확인 시각
- 연결된 시스템 구성요소가 없는 `INFO` 알림은 `componentCode`가 `null`이 될 수 있도록 처리했습니다.
- 존재하지 않는 알림 ID를 조회할 경우 `404 Not Found`를 반환하도록 처리했습니다.

### 알림 REST API 시간 형식 통일

- 관리자 Web의 시간 표시 기준을 통일하기 위해 알림 REST API의 시간 응답을 KST 기준 ISO 8601 형식으로 정리했습니다.
- 다음 시간 필드에 `+09:00` 오프셋을 포함하도록 처리했습니다.
  - `createdAt`
  - `checkedAt`
  - `recoveredAt`
- 적용 대상 API를 다음과 같이 정리했습니다.
  - 알림 목록 조회
  - 알림 상세 조회
  - 수동 알림 생성 응답
  - 알림 확인 처리 응답
  - 알림 복구 처리 응답
- 기존 MQTT 수신부에 영향을 주지 않도록 `smart_sorting/alert`의 MQTT `createdAt` 형식은 변경하지 않았습니다.

### 관리자 Web 알림 연동 기준 정리

- 관리자 Web의 알림/이상 내역 화면에서는 다음 REST API를 사용하도록 정리했습니다.

```text
GET   /api/alerts
GET   /api/alerts/summary
GET   /api/alerts/{alertId}
PATCH /api/alerts/{alertId}/check
PATCH /api/alerts/{alertId}/recover
```

- 알림 확인 및 복구 API는 별도의 Request Body 없이 URL의 `alertId`를 기준으로 처리하도록 구성했습니다.
- 관리자 Web에서는 `shortMessage`를 사용하지 않고 DB에 저장되는 `alertMessage`를 표시하도록 결정했습니다.
- `shortMessage`는 Qt 작업자 화면의 짧은 알림 표시용으로 유지했습니다.
- 수동 알림 생성 API `POST /api/alerts`는 현재 관리자 Web 기능에서는 사용하지 않도록 결정했습니다.
- 관리자 Web에서는 조회, 검색, 필터, 상세 조회, 확인, 복구 기능만 사용하도록 정리했습니다.

### 알림 실시간 연동 방식 정리

- 신규 알림은 기존 `smart_sorting/alert` MQTT Topic을 통해 관리자 Web에서 실시간으로 받을 수 있도록 기존 구조를 유지했습니다.
- MQTT 실시간 연동은 알림 **목록 화면의 신규 알림 반영 용도**로 사용하도록 정리했습니다.
- 알림 상세 화면에서는 MQTT를 사용하지 않고 REST API만 사용하도록 결정했습니다.
- 상세 화면 진입 시 `GET /api/alerts/{alertId}`를 통해 최신 상세 정보를 조회하도록 구성했습니다.
- 확인 및 복구 처리 후에는 PATCH 응답 또는 상세 API 재조회 결과를 이용해 화면 상태를 갱신할 수 있도록 정리했습니다.

### Web 연동 문서 정리

- 관리자 Web의 알림/이상 내역 화면 구현에 필요한 API 연동 기준을 별도 문서로 정리했습니다.
- 목록, 요약 통계, 상세 조회, 확인, 복구 API의 Endpoint와 상태값을 문서화했습니다.
- REST API의 KST 시간 형식과 MQTT 시간 형식의 차이를 명시했습니다.
- Web에서 사용하는 필드와 Qt 작업자 화면에서 사용하는 필드를 구분했습니다.

### 동작 테스트

- 제품 감지 목록 조회 및 페이지네이션 동작 확인
- 제품 유형 `CHOCOLATE`, `CANDY` 필터 동작 확인
- `FAILED` 분류 상태 필터 동작 확인
- 제품 감지 ID 검색 및 복합 조건 조회 동작 확인
- 잘못된 제품 유형 및 분류 상태 요청 시 `400 Bad Request` 반환 확인
- 제품 감지 목록 및 상세 응답의 `detectedAt`에 `+09:00` 오프셋이 포함되는 것을 확인
- 알림 목록 조회 및 10개 단위 페이지네이션 동작 확인
- `UNCHECKED`, `NOT_RECOVERED`, `RECOVERED` 상태 필터 동작 확인
- 장비 코드 및 알림 내용 검색 동작 확인
- 상태 필터와 검색 조건의 복합 조회 동작 확인
- 알림 목록 응답에 `componentCode`가 포함되는 것을 확인
- 알림 요약 통계의 미확인 및 미복구 건수가 목록 필터 결과와 일치하는 것을 확인
- 알림 상세 조회 정상 동작 및 존재하지 않는 ID의 `404 Not Found` 처리 확인
- 잘못된 알림 상태값 요청 시 `400 Bad Request` 반환 확인
- 알림 REST 응답의 `createdAt`, `checkedAt`, `recoveredAt`에 KST `+09:00` 오프셋이 적용되는 것을 확인

---

## 2026-09-03 — 제품 이미지 업로드 및 관리자 Web 이미지 연동 구현

### 관리자 Web MQTT 실시간 연동 확인

- 관리자 Web에서 서버가 Publish하는 MQTT Topic을 이용한 실시간 화면 갱신이 정상 동작하는 것을 확인했습니다.
- 다음 Topic의 Web 수신 구조를 기준으로 실시간 연동 상태를 확인했습니다.

```text
smart_sorting/product/detection
→ 신규 제품 감지 결과 실시간 반영

smart_sorting/alert
→ 신규 INFO / WARNING / ERROR 알림 목록 실시간 반영

smart_sorting/component/status
→ 시스템 구성요소 현재 상태 실시간 반영
```

- 알림 상세 화면은 기존 결정대로 MQTT를 사용하지 않고 REST API를 이용하도록 유지했습니다.
- 관리자 Web의 REST API 조회 기능과 MQTT 실시간 이벤트 처리 역할을 분리한 기존 구조를 유지했습니다.

### 제품 이미지 전달 방식 확정

- Raspberry Pi에서 촬영한 제품 이미지 자체를 MQTT Payload에 포함하지 않고 HTTP로 서버에 업로드하는 방식으로 정리했습니다.
- MQTT에는 Raspberry Pi의 로컬 이미지 경로가 아닌 서버가 반환한 `imagePath`만 전달하도록 기준을 확정했습니다.

```text
Raspberry Pi
        ↓
제품 이미지 촬영 및 로컬 저장
        ↓
HTTP 이미지 업로드
        ↓
ASP.NET Core Server
        ↓
서버 이미지 경로 반환
        ↓
제품 감지 MQTT Payload의 imagePath에 사용
```

- 기존 Raspberry Pi 로컬 경로는 관리자 Web에서 직접 접근할 수 없으므로 MQTT 저장 경로로 사용하지 않도록 정리했습니다.

```text
사용하지 않음
/home/rpi/smart_sorting/images/photo_001.jpg

사용
/images/products/서버파일명.jpg
```

### 제품 이미지 저장 디렉터리 구성

- ASP.NET Core 프로젝트에 제품 이미지 저장용 정적 파일 디렉터리를 추가했습니다.

```text
wwwroot/
└─ images/
   └─ products/
```

- 서버에 업로드된 제품 이미지는 `wwwroot/images/products` 아래에 저장하도록 구성했습니다.
- 이미지 파일은 DB에 Binary 데이터로 저장하지 않고 실제 파일은 서버 디스크에 저장하며, DB에는 이미지 상대 경로만 저장하는 기존 구조를 유지했습니다.

### ASP.NET Core 정적 파일 제공 설정

- 관리자 Web에서 저장된 제품 이미지에 HTTP로 접근할 수 있도록 `Program.cs`에 정적 파일 제공 Middleware를 추가했습니다.

```csharp
app.UseStaticFiles();
```

- 기존 Middleware 순서를 유지하면서 `UseCors` 이전에 정적 파일 제공 설정을 배치했습니다.

```text
UseHttpsRedirection
        ↓
UseStaticFiles
        ↓
UseCors
        ↓
UseAuthentication
        ↓
UseAuthorization
        ↓
MapControllers
```

- 이를 통해 서버에 저장된 제품 이미지를 다음 형식으로 직접 조회할 수 있도록 구성했습니다.

```text
http://서버IP:포트/images/products/파일명.jpg
```

### 제품 이미지 업로드 API 구현

- Raspberry Pi 제어부가 촬영한 이미지를 서버로 전송할 수 있도록 `ProductImagesController`를 추가했습니다.

```text
POST /api/product-images
```

- 요청은 `multipart/form-data` 형식을 사용하며 파일 Key는 `image`로 통일했습니다.

```text
Content-Type: multipart/form-data
Key: image
```

- 다음 이미지 확장자만 업로드할 수 있도록 검증을 추가했습니다.

```text
.jpg
.jpeg
.png
```

- 이미지 최대 업로드 크기를 `5MB`로 제한했습니다.
- 빈 파일 또는 이미지가 전달되지 않은 경우 `400 Bad Request`를 반환하도록 처리했습니다.
- 허용되지 않은 확장자 또는 최대 크기를 초과한 파일도 `400 Bad Request`로 처리하도록 구성했습니다.

### 서버 이미지 파일명 생성 및 저장

- Raspberry Pi에서 전달한 원본 파일명을 그대로 사용하지 않고 서버에서 고유한 파일명을 새로 생성하도록 구현했습니다.
- 날짜·시간과 GUID를 조합하여 동일 파일명 충돌을 방지하도록 구성했습니다.

```text
yyyyMMdd_HHmmss_GUID.jpg
```

- 서버는 이미지를 저장한 뒤 관리자 Web에서 사용할 수 있는 상대 경로를 반환하도록 구현했습니다.

```json
{
  "imagePath": "/images/products/20260903_155000_xxxxx.jpg"
}
```

### 이미지 업로드 API 동작 테스트

- Postman의 `form-data` 요청을 이용하여 실제 이미지 파일 업로드 테스트를 진행했습니다.
- 이미지 업로드 요청이 정상 처리되고 `200 OK` 응답과 `imagePath`가 반환되는 것을 확인했습니다.
- 업로드한 이미지 파일이 실제 `wwwroot/images/products` 디렉터리에 저장되는 것을 확인했습니다.
- 서버가 반환한 `imagePath`를 서버 Base URL과 결합하여 브라우저에서 직접 접근할 수 있는 것을 확인했습니다.

```text
http://서버IP:5051/images/products/파일명.jpg
```

### 제품 감지 MQTT와 이미지 경로 연동

- 이미지 업로드 API가 반환한 `imagePath`를 기존 제품 감지 MQTT Payload의 `imagePath` 필드에 사용하도록 연동 기준을 확정했습니다.

```json
{
  "classificationStatus": "SUCCESS",
  "productTypeCode": "CHOCOLATE",
  "confidence": 0.91,
  "imagePath": "/images/products/20260903_155000_xxxxx.jpg"
}
```

사용 Topic:

```text
smart_sorting/camera/product_detection
```

- 제품 감지 결과는 기존 `ProductDetectionService` 처리 구조를 그대로 사용하도록 유지했습니다.

```text
smart_sorting/camera/product_detection
        ↓
ProductDetectionService
        ↓
product_detections.image_path 저장
        ↓
smart_sorting/product/detection Publish
        ↓
관리자 Web
```

### 제품 이미지 전체 흐름 테스트

- Postman으로 서버에 이미지를 업로드하고 반환된 `imagePath`를 제품 감지 MQTT 메시지에 포함하여 전체 흐름을 테스트했습니다.
- 제품 감지 결과의 `imagePath`가 `product_detections`에 저장되는 것을 확인했습니다.
- 서버가 Web용 `smart_sorting/product/detection` Topic으로 동일한 이미지 경로를 전달하는 구조를 확인했습니다.
- 관리자 Web에서 실제 업로드된 제품 이미지가 정상 표시되는 것을 확인했습니다.

```text
이미지 업로드
        ↓
서버 파일 저장
        ↓
imagePath 반환
        ↓
camera/product_detection
        ↓
DB 저장
        ↓
product/detection Publish
        ↓
관리자 Web 실제 이미지 표시
```

### 제어부 이미지 업로드 연동 기준 정리

- Raspberry Pi 제어부에서 Python `requests`를 이용해 이미지 업로드 API를 호출하는 기준을 정리했습니다.

```python
with open(local_image_path, "rb") as image_file:
    response = requests.post(
        f"{SERVER_URL}/api/product-images",
        files={
            "image": image_file
        }
    )

response.raise_for_status()

server_image_path = response.json()["imagePath"]
```

- 서버에서 반환한 `imagePath`를 기존 MQTT 제품 감지 Payload에 포함하도록 제어부 처리 순서를 정리했습니다.

```text
1. 제품 이미지 촬영
2. Raspberry Pi 로컬 이미지 저장
3. POST /api/product-images 호출
4. imagePath 응답 수신
5. 제품 감지 MQTT Payload에 imagePath 설정
6. smart_sorting/camera/product_detection Publish
```

- 제어부 전달용 `CONTROL_IMAGE_UPLOAD_GUIDE.md` 문서를 별도로 작성했습니다.

### 서버 Base URL 및 외부 접근 기준 확인

- 현재 `launchSettings.json`의 HTTP 서버 Listen 주소를 확인했습니다.

```text
http://0.0.0.0:5051
```

- 서버 PC 내부 테스트에서는 다음 Base URL을 사용할 수 있습니다.

```text
http://localhost:5051
```

- Raspberry Pi 또는 다른 PC에서 접근할 때는 서버 PC의 실제 IPv4 주소를 사용하도록 정리했습니다.

```text
http://서버PC_IP:5051
```

이미지 업로드 API 예시:

```text
http://서버PC_IP:5051/api/product-images
```

제품 이미지 조회 예시:

```text
http://서버PC_IP:5051/images/products/파일명.jpg
```

---

## 2026-09-04 

---

## 현재 완료 상태

- [x] 데이터베이스 생성 스크립트
- [x] 7개 테이블 정의
- [x] 기본 키·외래키·고유 키 정의
- [x] NULL 허용 여부 정의
- [x] CHECK 제약조건 정의
- [x] 제품 유형 초기 데이터
- [x] 시스템 구성요소 초기 데이터
- [x] 테스트용 더미 데이터
- [x] DBeaver ERD
- [x] ERDCloud ERD

### ASP.NET Core / DB

- [x] ASP.NET Core Web API 프로젝트 생성
- [x] MySQL 및 Entity Framework Core 연동
- [x] Entity 모델 및 `AppDbContext` 작성
- [x] `ProductionTarget` 모델 및 `production_targets` 매핑
- [x] 관리자 웹 개발용 CORS 설정
- [x] `system_components` 구성요소 기준 재정리
- [x] `VISION_MODULE` 제거 및 `CAMERA` 기준 통일
- [x] `WORKER_DISPLAY` 포함 13개 Component 초기 데이터 반영
- [x] 시스템 구성요소 초기 상태 `OFFLINE` 기준 적용
- [x] 초콜릿·사탕 생산 목표를 모두 세트 단위로 통일
- [x] `target_candy_count`를 `target_candy_set_count`로 변경
- [x] `TargetCandyCount`를 `TargetCandySetCount`로 변경
- [x] 변경된 생산 목표 구조에 맞춰 DB 스키마 재생성
- [x] `ProductionTarget`, `ProductionSession` Entity 수정
- [x] `AppDbContext` Fluent API 매핑 수정
- [x] 실제 생산 수량은 낱개 단위, 목표는 세트 단위로 분리
- [x] `daily_worker_count` 컬럼 추가 및 현재 작업 인원 DB 관리
- [x] `next_daily_worker_count` 컬럼 추가 및 예약 작업 인원 DB 관리
- [x] `ProductionTarget.NextDailyWorkerCount` 및 EF Core 매핑 추가

### 인증

- [x] BCrypt 기반 로그인 API 구현
- [x] 관리자 테스트 계정 BCrypt 해시 적용
- [x] JWT 발급 및 인증 처리
- [x] 로그인 성공 업무 로그 추가

### 생산 목표

- [x] 생산 목표 조회 API
- [x] 생산 목표 설정 API
- [x] 생산 목표 조회·설정 API 테스트
- [x] 생산 목표 변경 업무 로그 추가
- [x] 초콜릿·사탕 목표 필드를 `SetCount` 기준으로 통일
- [x] `targetChocolateSetCount`, `targetCandySetCount` 요청·응답 구조 적용
- [x] `unit_per_set` 기반 목표 낱개 수 계산 적용
- [x] 목표 세트 수와 실제 목표 낱개 수 계산 구조 검증
- [x] 생산 목표 변경 후 DB 저장 결과 확인
- [x] 하루 작업 인원을 DB 값 기준으로 관리하도록 변경
- [x] 작업 인원 설정 API `PUT /api/production-targets/worker-count` 구현
- [x] 생산 시작 전 생산 목표 즉시 적용
- [x] 생산 시작 후 다음 날 생산 목표 예약
- [x] 생산 시작 전 작업 인원 즉시 적용
- [x] 생산 시작 후 다음 날 작업 인원 예약
- [x] 예약 생산 목표와 예약 작업 인원 상호 검증
- [x] 예약 생산 목표·작업 인원 서버 재시작 후 유지 확인
- [x] 다음 날 예약 생산 목표·작업 인원 적용 시뮬레이션 테스트
- [x] 예약값 적용 후 `next_\\*` 필드 `NULL` 초기화 확인
- [x] 생산 목표 API와 작업 인원 API 역할 분리
- [x] 관리자 웹용 생산 목표·작업 인원 연동 기준 문서화

### 생산 작업

- [x] 생산 작업 시작 API
- [x] 현재 생산 작업 조회 API
- [x] 생산 작업 종료 API
- [x] 생산 작업 시작 API 사용자 Claim 조회 오류 수정
- [x] 활성 생산 작업 중복 생성 방지
- [x] 생산 작업 종료 시 목표 달성 여부 판단
- [x] 생산 작업 시작·종료 업무 로그 추가
- [x] 생산 세션 목표를 세트 단위로 저장하도록 수정
- [x] 생산 시작 시 `unit_per_set`을 이용한 목표 낱개 수 계산
- [x] 생산 종료 시 `unit_per_set`을 이용한 목표 달성 여부 계산
- [x] 목표 미달성 시 `CANCELLED` 처리 테스트
- [x] 목표 달성 시 `COMPLETED` 처리 테스트
- [x] 종료 시 제품별 진행률 `100%` 계산 확인
- [x] 생산 시작·종료 MQTT Payload의 목표량 계산 확인
- [x] `started_at` 기준 오늘 생성된 생산 세션 수 조회 로직 추가
- [x] 하루 최대 생산 세션 수를 작업 인원 기준으로 제한
- [x] 오늘 세션 순번 계산 로직 추가
- [x] 하루 목표를 작업 인원 수 기준으로 나누는 목표 계산 로직 추가
- [x] 나머지 목표를 앞 세션부터 1개씩 분배하는 로직 추가
- [x] 계산된 세션별 목표를 `production_sessions`에 저장하도록 수정
- [x] 작업 인원 변경 후 세션별 목표 분배 동작 확인
- [x] 작업 인원 수만큼 세션 생성 후 추가 세션 생성 차단 확인
- [x] 오늘 첫 세션 시작 시 예약 생산 목표 자동 적용
- [x] 오늘 첫 세션 시작 시 예약 작업 인원 자동 적용

### 제품 감지

- [x] 제품 감지 결과 저장 API
- [x] 제품 분류 성공·실패 검증
- [x] 초콜릿·사탕 생산 수량 갱신 로직
- [x] 제품 감지 처리 로직 `ProductDetectionService` 분리
- [x] 제품 분류 성공·실패 업무 로그 추가
- [x] 초콜릿 세트 완료 시 `INFO` 알림 생성
- [x] 제품 분류 MQTT Topic을 `smart_sorting/camera/product_detection`으로 변경
- [x] 제품 분류 `FAILED`와 실제 장비 상태 처리 분리
- [x] 제품 분류 `FAILED` 시 `CAMERA = ERROR` 자동 변경 제거
- [x] 제품 분류 `FAILED` 시 자동 `ERROR` 알림 생성 제거
- [x] 제품 분류 `FAILED` 처리 재테스트
- [x] `CHOCOLATE / SUCCESS` 수신 시 생산량 증가 확인
- [x] `CANDY / SUCCESS` 수신 시 생산량 증가 확인
- [x] 제품 감지 후 `currentCount`, `setCount`, `progress` MQTT 갱신 확인
- [x] 사탕 `unit_per_set = 5` 임시 변경을 통한 계산 로직 검증
- [x] 테스트 후 사탕 `unit_per_set = 1` 복원
- [x] 서버 → 관리자 Web 신규 제품 감지 Topic `smart_sorting/product/detection` Publish 구현
- [x] `SUCCESS / FAILED` 감지 결과 모두 `product/detection`으로 실시간 전달
- [x] `CHOCOLATE / CANDY / FAILED` 실시간 제품 감지 Publish 테스트
- [x] 초콜릿 세트 완료 INFO 판단을 현재 세션 기준에서 당일 전체 누적 기준으로 변경
- [x] 작업자 교대 후 새 세션이 0부터 시작해도 이전 생산량과 합산하여 세트 완료 INFO가 이어지도록 처리
- [x] 당일 누적 120개 도달 시 `초콜릿 12세트 완료` INFO Publish 테스트
- [x] 작업자별 `production_sessions.chocolate_count` 실적은 세션별로 유지되는 구조 확인
- [x] 관리자 Web 제품 감지 전체 내역 조회 API 구현
- [x] 제품 감지 목록 최신순 조회 및 Pagination 구조 확정
- [x] 제품 유형 `CHOCOLATE / CANDY` 필터 구현
- [x] 분류 상태 `FAILED` 필터 구현
- [x] 감지 ID 정확 일치 검색 구현
- [x] 잘못된 제품 유형·분류 상태·감지 ID 검색값 `400 Bad Request` 처리
- [x] 제품 감지 상세 조회 API `GET /api/product-detections/{productDetectionId}` 구현
- [x] 제품 감지 목록·상세 `detectedAt`을 KST `+09:00` ISO 8601 형식으로 통일
- [x] 제품 감지 Web 연동용 Nullable 필드 기준 정리

### 시스템 구성요소

- [x] 시스템 구성요소 상태 조회 API
- [x] 시스템 구성요소 상태 변경 API
- [x] 실제 상태가 변경된 경우에만 `component/status` MQTT Publish
- [x] 시스템 구성요소 상태 변경 업무 로그 추가
- [x] 알림 생성에 따른 구성요소 상태 변경 로그 추가
- [x] 알림 복구에 따른 구성요소 상태 변경 로그 추가
- [x] 작업자 LCD용 `WORKER_DISPLAY` Component 반영
- [x] 서버 DB 및 관련 코드의 `VISION_MODULE`을 `CAMERA`로 변경
- [x] 실제 장비 상태 수신 시 `system_components.current_status` 갱신 구현
- [x] 실제 장비 상태 변경 시 `status_updated_at` 갱신 구현
- [x] 실제 상태가 변경된 경우에만 `[COMPONENT]` 로그 출력

### 알림

- [x] 알림 생성 API
- [x] 알림 조회 API
- [x] 알림 확인 처리 API
- [x] 알림 복구 처리 API
- [x] 알림 유형 및 우선순위 조합 검증
- [x] 수동 알림과 시스템 구성요소 상태 연동
- [x] 알림과 현재 생산 세션 연결
- [x] 알림과 제품 감지 결과 연결 구조 구현
- [x] 동일 구성요소의 미복구 알림을 고려한 상태 복구 처리
- [x] 수동 알림 생성 시 MQTT Publish 연동
- [x] 알림 복구 시 시스템 구성요소 상태 재계산
- [x] 알림 복구에 따른 `component/status` MQTT Publish
- [x] 알림 생성 업무 로그 추가
- [x] 알림 확인 업무 로그 추가
- [x] 알림 복구 업무 로그 추가
- [x] 알림 생성 → 구성요소 `NORMAL -> ERROR` 테스트
- [x] 알림 복구 → 구성요소 `ERROR -> NORMAL` 테스트
- [x] 제품 분류 `FAILED`와 자동 장비 오류·알림 생성을 분리하도록 구조 수정
- [x] `alerts.error_code` 컬럼 추가 및 `Alert.ErrorCode` Entity 매핑
- [x] 알림 조회 API에 `errorCode` 응답 추가
- [x] `ComponentAlertService` 분리 및 Error Code 기반 Alert 정보 매핑 구현
- [x] 동일 Component + 동일 `errorCode`의 미복구 Alert 중복 생성 방지
- [x] Component `NORMAL` 복구 시 자동 생성 Alert `RECOVERED` 처리
- [x] 수동 Alert는 자동 복구 대상에서 제외하도록 구분
- [x] 알림 MQTT Payload에 `errorCode`, `shortMessage`, `alertMessage` 적용
- [x] Qt용 `shortMessage`와 Web/DB용 `alertMessage` 역할 분리
- [x] 수동 알림 생성 시 `shortMessage` 필수 및 50자 이하 검증 추가
- [x] INFO 알림 MQTT에도 공통 알림 Payload 구조 적용
- [x] `CAMERA_ERROR`, `NO_DETECTION`, `SERIAL_DISCONNECTED`, `NORMAL` 복구 시나리오 테스트
- [x] 관리자 Web 알림 목록 10개 단위 Pagination 구현
- [x] 알림 목록 응답에 `componentCode` 추가
- [x] 알림 상태 필터 `UNCHECKED / NOT_RECOVERED / RECOVERED` 구현
- [x] 잘못된 알림 상태값 `400 Bad Request` 처리
- [x] 장비 코드 또는 알림 내용 검색 기능 구현
- [x] 상태 필터와 검색 조건 복합 조회 테스트
- [x] 알림 요약 통계 API `GET /api/alerts/summary` 구현
- [x] 오늘 발생 / 미확인 / 미복구 집계 기준 확정
- [x] 알림 상세 조회 API `GET /api/alerts/{alertId}` 구현
- [x] 알림 목록·상세·확인·복구 REST 시간 응답을 KST `+09:00` ISO 8601 형식으로 통일
- [x] MQTT `createdAt`은 기존 형식 유지로 결정
- [x] 관리자 Web은 `alertMessage`, Qt는 `shortMessage` 사용 기준 유지
- [x] 알림 확인·복구 PATCH API는 Request Body 없이 호출하는 구조 확정
- [x] 수동 알림 생성 API는 현재 관리자 Web 미사용으로 결정

### MQTT

- [x] 외부 HTTP API 접속 확인
- [x] Mosquitto 외부 접속 확인
- [x] MQTT Broker와 ASP.NET Core 서버 연결
- [x] MQTT 제품 감지 Subscribe 구현
- [x] MQTT 제품 감지 결과 DB 저장 및 생산량 갱신 테스트
- [x] MQTT 제품 감지 Topic 이름 정리
- [x] 서버 → 클라이언트 MQTT Publish 구현
- [x] `smart_sorting/production/status` Publish 구현
- [x] `smart_sorting/alert` Publish 구현
- [x] `smart_sorting/component/status` Publish 구현
- [x] 생산 작업 시작·종료 시 `production/status` MQTT Publish 구현
- [x] Mosquitto WebSocket `9001` Listener 구성
- [x] 외부 네트워크에서 WebSocket 포트 연결 확인
- [x] MQTT Publish 로그 구조 정리
- [x] MQTT Receive 로그 구조 정리
- [x] MQTT Payload 전체 콘솔 출력 제거
- [x] MQTT 연결·수신 처리 오류 `ILogger` 적용
- [x] `IHostApplicationLifetime`을 이용한 MQTT 연결 시작 시점 조정
- [x] 서버 시작 로그 이후 MQTT 연결 로그 출력 확인
- [x] `smart_sorting/component/status/update` Subscribe 구현
- [x] Component 상태 메시지 Topic 분기 처리 구현
- [x] Component 상태 MQTT 메시지 DB 반영 테스트
- [x] 생산 현황 MQTT의 `targetCount`를 세트 목표 × `unit_per_set` 기준으로 통일
- [x] 생산 현황 MQTT의 `setCount`, `progress` 계산 기준 통일
- [x] `smart_sorting/product/detection` Publish 구현
- [x] 제품 감지 DB 저장 직후 Web용 실시간 감지 결과 Publish
- [x] `smart_sorting/alert` Payload를 자동/수동/INFO 알림 공통 구조로 통일
- [x] Component 이상 상태 수신 → DB 갱신 → Alert 생성 → `component/status` / `alert` Publish 연동
- [x] Component `NORMAL` 복구 → 자동 Alert 복구 → `component/status = NORMAL` Publish 연동
- [x] MQTT Explorer를 이용한 장비 상태·알림 주요 시나리오 테스트

### 관리자 대시보드

- [x] 관리자 대시보드 오늘 생산량 요약 API
- [x] 관리자 대시보드 시간대별 생산량 추이 API
- [x] 관리자 대시보드 제품 분류 비율 API
- [x] 관리자 대시보드 최근 제품 감지 결과 API
- [x] 최근 제품 감지 결과를 전체 기록 기준 최신 5개로 조회하도록 수정
- [x] 제품 분류 비율을 `SUCCESS / FAILED`에서 `CHOCOLATE / CANDY` 기준으로 수정
- [x] 제품 분류 비율 계산에서 `FAILED` 결과 제외

### 클라이언트 통신 구조

- [x] 작업자 UI 통신 구조 큰 틀 정리
- [x] 관리자 웹 통신 구조 큰 틀 정리
- [x] 관리자 웹·작업자 Qt REST API 역할 분리
- [x] 작업자·관리자 공통 MQTT Payload 구조 정리
- [x] 관리자 웹 생산 목표·작업 인원 현재값/예약값 표시 기준 정리
- [x] 관리자 웹 생산 목표·작업 인원 REST API 연동 문서 작성
- [x] Qt 작업자 화면 Alert MQTT 변경사항 연동 문서 작성
- [x] 관리자 Web 최근 제품 감지 실시간 MQTT 연동 문서 작성
- [x] 관리자 Web Alert MQTT 변경사항 연동 문서 작성
- [x] Qt는 `shortMessage`, 관리자 Web은 `alertMessage` 중심으로 사용하는 기준 확정
- [x] REST는 초기/이력 조회, MQTT는 신규 실시간 이벤트 처리로 역할 분리
- [x] 관리자 Web 제품 감지 내역 조회·필터·검색·상세 REST 연동 기준 정리
- [x] 관리자 Web 알림 목록·요약·검색·필터·상세·확인·복구 REST 연동 기준 정리
- [x] 알림 목록 화면은 MQTT 신규 알림 실시간 반영, 상세 화면은 REST 전용으로 역할 분리
- [x] 관리자 Web 알림/이상 내역 API 연동 문서 작성

### NLog / 서버 로그

- [x] EF Core SQL 로그 출력 축소
- [x] `NLog.Web.AspNetCore` 설치
- [x] `nlog.config` 추가
- [x] NLog Console 출력 형식 `${message}` 적용
- [x] `Microsoft.Hosting.Lifetime` 시작·종료 로그 유지
- [x] ASP.NET Core Routing / MVC / Endpoint Information 로그 숨김
- [x] EF Core 내부 Information 로그 숨김
- [x] 불필요한 Data Protection Information 로그 제거
- [x] 서버 업무 로그 표시 Rule 구성
- [x] `[LOGIN]` 로그 추가
- [x] `[TARGET]` 로그 추가
- [x] `[SESSION]` 로그 추가
- [x] `[DETECTION]` 로그 추가
- [x] `[ALERT]` 로그 추가
- [x] `[COMPONENT]` 로그 추가
- [x] `[MQTT]` 로그 추가

### 실제 장비 상태 MQTT 규격

- [x] 실제 장비 → 서버 상태 MQTT Topic 구조 정리
- [x] `smart_sorting/component/status/update` Topic 사용 결정
- [x] 실제 장비 상태와 `system_components` 연동 기준 정리
- [x] 제품 분류 결과와 장비 상태 메시지 역할 분리
- [x] `componentCode`, `status`, `errorCode` Payload 구조 확정
- [x] 허용 상태값 `NORMAL`, `WARNING`, `ERROR`, `OFFLINE` 정리
- [x] `NO_DETECTION`을 `CAMERA / WARNING`으로 처리하는 기준 정리
- [x] Hardware / Software Error Code 정리
- [x] Alert Priority `LOW / MEDIUM / HIGH` 기준 정리
- [x] 정상 상태에서 `errorCode = null` 기준 정리
- [x] 제품 분류 결과와 Component 상태가 동시에 발생하는 상황 처리 기준 정리
- [x] 제품 분류와 직접 관계없는 Component 오류 처리 기준 분리
- [x] `CONTROL_INTERFACE_SPEC.md` 작성
- [x] 제어부 → 서버 MQTT Payload 예시 문서화
- [x] 서버 공통 인프라 상태 판단 기준을 제어부용 인터페이스 문서에서 분리

---

## 미완료 상태

### 생산 목표 / 생산 작업

- [ ] 세션별 목표값이 작업자 Qt 화면에 정상 표시되는지 실제 연동 확인
- [ ] 관리자 웹의 하루 목표 / 하루 누적 생산량 / 진행률 실제 화면 연동 확인
- [ ] 관리자 대시보드 오늘 생산량 요약 API가 작업자 교대 및 현재 목표 구조와 일치하는지 최종 확인
- [ ] 관리자 웹 생산 목표·작업 인원 설정 API 실제 화면 연동 확인
- [ ] 작업 인원 변경 및 다음 날 예약 상태가 관리자 화면에 정상 표시되는지 확인

### 관리자 Web 상세 기능

- [ ] 알림 상세 화면 실제 Web 연동
- [ ] 제품 감지 내역 상세 화면 실제 Web 연동
- [ ] 최근 제품 감지 이미지 실제 경로 제공 방식 최종 연동
- [ ] 시간대별 생산량 그래프 목표선 표시 기준 확정

### MQTT 안정성 / 클라이언트 연동

- [ ] MQTT Broker 연결 끊김 및 재연결 처리 보강
- [ ] MQTT Publisher / Subscriber 예외 및 재시도 처리 보강
- [ ] 관리자 Web `smart_sorting/product/detection` 실제 화면 연동
- [ ] 관리자 Web `smart_sorting/alert` 실제 화면 연동
- [ ] 관리자 Web `smart_sorting/component/status` 실제 화면 연동
- [ ] 작업자 Qt REST API 실제 연동
- [ ] 작업자 Qt `smart_sorting/alert` 및 `component/status` 실제 연동
- [ ] 작업자 UI 라인 제어 MQTT 연동
- [ ] Raspberry Pi 및 전체 클라이언트 통합 테스트

### 실제 장비 / 통합 테스트

- [ ] Raspberry Pi 제어부 실제 `component/status/update` 메시지 수신 테스트
- [ ] 실제 카메라·Arduino 오류 발생 및 복구 흐름 통합 테스트
- [ ] 실제 제품 감지 이미지 경로와 Web 표시 연동
- [ ] 작업자 교대가 포함된 전체 생산 흐름 End-to-End 테스트

---

## 다음 작업

### 1. 관리자 Web 생산 목표·작업 인원 실제 연동

서버의 생산 목표와 작업 인원 설정 기능은 현재값과 예약값을 모두 지원하도록 구현 및 테스트가 완료된 상태다.

```text
GET /api/production-targets/current

→ 현재/예약 생산 목표 및 작업 인원 조회

PUT /api/production-targets/current

→ 생산 목표 설정 또는 다음 날 예약

PUT /api/production-targets/worker-count

→ 작업 인원 설정 또는 다음 날 예약
```

확인할 항목:

- 생산 시작 전 생산 목표 즉시 변경
- 생산 시작 후 다음 날 생산 목표 예약 표시
- 생산 시작 전 작업 인원 즉시 변경
- 생산 시작 후 다음 날 작업 인원 예약 표시
- 서버 응답 `message` 사용자 알림 처리
- 페이지 재조회 시 DB 기준 값 재표시

---

### 2. 관리자 Web 하루 생산 현황 최종 연동

관리자 Web은 현재 작업자 개인 세션이 아니라 하루 전체 공정 생산 현황을 표시한다.

```text
production_targets

→ 하루 전체 목표

production_sessions

→ 작업자별 생산 실적

오늘 production_sessions 합계

→ 하루 누적 생산량
```

작업자별 DB 실적은 각 세션에 유지하고, 관리자 Web에서는 오늘 세션 수량을 합산해 표시한다.

---

### 3. 작업자 Qt 실제 연동

작업자 Qt는 현재 작업자 세션 기준으로 생산 현황을 표시한다.

```text
하루 전체 생산 목표
        ↓
daily_worker_count 기준 분배
        ↓
production_sessions
        ↓
현재 작업자 세션 목표 / 실적
        ↓
작업자 Qt
```

확인할 항목:

- 현재 세션 목표 표시
- 현재 세션 생산량 표시
- 세션별 진행률 표시
- `smart_sorting/alert`의 `shortMessage` 표시
- `smart_sorting/component/status` 상태 반영
- 작업자 교대 시 새 세션 생산량 0부터 시작

초콜릿 세트 완료 INFO는 서버가 당일 전체 누적 기준으로 계산한 메시지를 Qt에서 그대로 표시한다.

---

### 4. 관리자 Web 실시간 MQTT 연동

```text

smart_sorting/product/detection

→ 최근 제품 감지 결과 실시간 갱신

smart_sorting/alert

→ 알림 목록의 신규 INFO / WARNING / ERROR 실시간 갱신

→ 알림 상세 화면은 REST API만 사용

smart_sorting/component/status

→ 장비 현재 상태 실시간 갱신

```

기존 REST API는 최초 화면 진입 및 전체 이력 조회 용도로 유지한다.

---

### 5. 관리자 Web 상세 화면 실제 연동

알림/제품 감지 상세 조회에 필요한 서버 REST API와 조회 구조는 구현이 완료된 상태다.

```text

알림 상세

→ GET /api/alerts/{alertId}

→ Alert 상세 정보

→ Error Code

→ 발생/확인/복구 상태 및 시간

→ 관련 Component / 생산 세션 / 제품 감지 정보

→ 상세 화면은 MQTT 없이 REST API만 사용

제품 감지 내역 상세

→ GET /api/product-detections

→ GET /api/product-detections/{productDetectionId}

→ 전체 제품 감지 기록 최신순 조회

→ 제품 유형 / FAILED 필터

→ 감지 ID 검색

→ 신뢰도 / 이미지 경로 / 감지 시간

→ Pagination

```

남은 작업:

- 관리자 Web 실제 상세 화면 연동
- 제품 이미지 실제 경로 제공 및 표시 방식 최종 연동

---

### 6. MQTT 연결 안정성 보강

확인 항목:

- MQTT Broker 연결 끊김 감지
- Subscriber 재연결
- Topic 재구독
- Publisher 연결 실패 처리
- Publish 실패 및 재시도 처리

---

### 7. 실제 Raspberry Pi / Arduino 연동

```text

제어부

→ smart_sorting/component/status/update

→ ASP.NET Core Server

→ system_components 상태 갱신

→ Error Code 기반 Alert 생성

→ DB 저장

→ component/status Publish

→ alert Publish

```

주요 확인 대상:

- `NO_DETECTION`
- `CAMERA_ERROR`
- `SERIAL_DISCONNECTED`
- Servo / Conveyor / IR / Buzzer 오류
- `NORMAL` 복구

---

### 8. 전체 시스템 통합 테스트

```text
Raspberry Pi / Arduino 제어부
        ↓
MQTT Broker
        ↓
ASP.NET Core Server
   ┌───┴───┐
   ↓              ↓
MySQL        MQTT Publish
                   ↓
             ┌──┴──┐
             ↓          ↓
       관리자 Web    작업자 Qt
```

최종 확인:

- 하루 생산 목표 및 작업 인원 설정
- 다음 날 목표·작업 인원 예약
- 작업자별 세션 목표 분배
- 작업자 로그인 및 생산 세션 시작
- 제품 감지 및 분류 결과 저장
- 작업자별 생산 실적 저장
- 작업자 교대
- 당일 전체 누적 기준 초콜릿 세트 INFO
- Component 이상 상태 및 Error Code 기반 Alert
- Component 정상 복구
- 관리자 Web 실시간 상태 반영
- 작업자 Qt 실시간 상태 반영
- 실제 제품 이미지 표시
- 생산 작업 종료
- DB 최종 기록 확인
