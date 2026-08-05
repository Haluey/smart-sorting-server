# Smart Sorting System 데이터베이스 설계

컨베이어 기반 초콜릿·사탕 자동 분류 시스템에서 사용하는 MySQL 데이터베이스의 구조와 설계 기준을 정리한 문서입니다.

---

## 1. ERD

![스마트 분류 시스템 ERD](../docs/smart_sorting_system_erd.png)

관련 파일:

- SQL 스키마: `smart_sorting_system.sql`
- DBeaver 편집용 ERD: `../docs/smart_sorting_system.erd`
- GitHub 열람용 ERD: `../docs/smart_sorting_system_erd.png`

---

## 2. 설계 개요

데이터베이스는 `production_sessions`를 중심으로 다음 정보를 연결합니다.

- 생산 작업을 수행한 사용자
- 생산 작업 중 감지된 개별 제품
- 초콜릿과 사탕의 제품 유형
- 하드웨어·소프트웨어 구성요소의 현재 상태
- 생산 및 시스템 과정에서 발생한 알림

`alerts`는 생산 작업, 제품 감지 결과, 시스템 구성요소와 각각 독립적으로 연결될 수 있으며, 생산과 무관한 서버·데이터베이스 알림도 저장할 수 있도록 관련 외래키에 `NULL`을 허용합니다.

모든 관계는 자식 테이블이 독립적인 기본 키를 가지는 비식별 관계로 구성했습니다.

---

## 3. 테이블 구성

### 3.1 `users` — 사용자

작업자와 관리자의 로그인 정보 및 권한을 관리합니다.

| 컬럼 | 설명 |
|---|---|
| `user_id` | 사용자 번호 |
| `login_id` | 로그인 아이디 |
| `password_hash` | BCrypt 비밀번호 해시 |
| `name` | 사용자명 |
| `role` | `ADMIN`, `WORKER` |
| `created_at` | 계정 생성 일시 |

주요 규칙:

```text
login_id = UNIQUE
role = ADMIN 또는 WORKER
```

---

### 3.2 `product_types` — 제품 유형

초콜릿과 사탕의 기준 정보를 관리합니다.

| 컬럼 | 설명 |
|---|---|
| `product_type_id` | 제품 유형 번호 |
| `product_type_code` | 서버용 고정 식별 코드 |
| `product_name` | 제품명 |
| `unit_per_set` | 1세트 구성 개수 |
| `created_at` | 생성 일시 |

초기 데이터:

| 코드 | 제품명 | 세트당 개수 |
|---|---|---:|
| `CHOCOLATE` | 초콜릿 | 10 |
| `CANDY` | 사탕 | 1 |

`unit_per_set`은 0보다 커야 합니다.

---

### 3.3 `system_components` — 시스템 구성요소

하드웨어와 소프트웨어 구성요소의 현재 상태를 관리합니다.

| 컬럼 | 설명 |
|---|---|
| `component_id` | 구성요소 번호 |
| `component_code` | API·MQTT 연동용 고정 코드 |
| `component_name` | 화면 표시 이름 |
| `component_type` | 구성요소 유형 |
| `current_status` | 현재 상태 |
| `status_updated_at` | 상태 갱신 일시 |

구성요소 유형:

```text
SENSOR
ACTUATOR
CONTROLLER
DISPLAY
SOFTWARE
SERVER
DATABASE
```

현재 상태:

```text
NORMAL
WARNING
ERROR
OFFLINE
```

초기 등록 구성요소:

- Raspberry Pi 5
- Arduino 제어 보드
- 제품 투입 감지 센서
- 제품 분류 카메라
- 컨베이어 벨트
- 제품 분류 서보모터
- 알림 부저
- 작업자 LCD 장비
- OpenCV 제품 분류 모듈
- 작업자 화면 프로그램
- 관리자 웹 프로그램
- MQTT 브로커
- ASP.NET Core API 서버
- MySQL 데이터베이스

`status_updated_at`은 최초 등록 시 자동 입력하고, 상태 변경 시 서버에서 `current_status`와 함께 갱신합니다.

---

### 3.4 `production_sessions` — 생산 작업

작업자 한 명이 수행하는 하나의 생산 작업 단위를 관리합니다.

| 컬럼 | 설명 |
|---|---|
| `session_id` | 생산 작업 번호 |
| `user_id` | 작업자 번호 |
| `target_chocolate_set_count` | 목표 초콜릿 세트 수 |
| `target_candy_count` | 목표 사탕 수 |
| `chocolate_count` | 생산된 초콜릿 낱개 수 |
| `candy_count` | 생산된 사탕 수 |
| `status` | 생산 작업 상태 |
| `started_at` | 작업 시작 일시 |
| `ended_at` | 작업 종료 일시 |
| `updated_at` | 마지막 수정 일시 |

상태:

```text
RUNNING
PAUSED
COMPLETED
CANCELLED
```

주요 규칙:

- 목표량과 생산량은 음수가 될 수 없습니다.
- 초콜릿과 사탕 목표량이 모두 0인 생산 작업은 생성할 수 없습니다.
- `RUNNING`, `PAUSED`에서는 `ended_at`이 `NULL`이어야 합니다.
- `COMPLETED`, `CANCELLED`에서는 `ended_at`이 필요합니다.

생산 날짜는 `DATE(started_at)`으로 구하고, 초콜릿 완성 세트 수는 `chocolate_count DIV unit_per_set`으로 계산합니다.

---

### 3.5 `product_detections` — 제품 감지 및 분류 결과

제품이 감지되고 촬영·분류될 때마다 한 행씩 저장합니다.

| 컬럼 | 설명 |
|---|---|
| `product_detection_id` | 제품 감지 번호 |
| `session_id` | 생산 작업 번호 |
| `product_type_id` | 제품 유형 번호 |
| `confidence` | 분류 신뢰도 |
| `image_path` | 촬영 이미지 경로 |
| `classification_status` | 분류 상태 |
| `detected_at` | 감지 일시 |

분류 상태:

```text
SUCCESS
FAILED
```

주요 규칙:

- 신뢰도는 0 이상 1 이하여야 합니다.
- `SUCCESS`이면 `product_type_id`와 `confidence`가 필요합니다.
- `FAILED`이면 `product_type_id`는 `NULL`이어야 합니다.
- 촬영·분류 실패를 저장할 수 있도록 일부 컬럼에 `NULL`을 허용합니다.

---

### 3.6 `alerts` — 알림

생산 과정과 시스템에서 발생한 정보, 경고, 오류의 이력을 저장합니다.

| 컬럼 | 설명 |
|---|---|
| `alert_id` | 알림 번호 |
| `session_id` | 관련 생산 작업 번호 |
| `component_id` | 관련 구성요소 번호 |
| `product_detection_id` | 관련 제품 감지 번호 |
| `checked_by_user_id` | 알림을 확인한 사용자 번호 |
| `alert_type` | 알림 유형 |
| `priority` | 중요도 |
| `recovery_status` | 복구 상태 |
| `check_status` | 확인 상태 |
| `alert_message` | 알림 상세 메시지 |
| `created_at` | 발생 일시 |
| `recovered_at` | 복구 일시 |
| `checked_at` | 확인 일시 |

알림 유형:

```text
INFO
WARNING
ERROR
```

중요도:

```text
LOW
MEDIUM
HIGH
```

허용 조합:

| 알림 유형 | 허용 중요도 |
|---|---|
| `INFO` | `LOW` |
| `WARNING` | `MEDIUM`, `HIGH` |
| `ERROR` | `LOW`, `MEDIUM`, `HIGH` |

복구 상태:

```text
NOT_RECOVERED
RECOVERED
```

확인 상태:

```text
UNCHECKED
CHECKED
```

주요 규칙:

- `INFO`는 복구와 확인 대상이 아니므로 관련 값이 `NULL`입니다.
- `RECOVERED`이면 `recovered_at`이 필요합니다.
- `NOT_RECOVERED`이면 `recovered_at`은 `NULL`입니다.
- `CHECKED`이면 `checked_by_user_id`와 `checked_at`이 필요합니다.
- `UNCHECKED`이면 확인 사용자와 확인 시각은 `NULL`입니다.

알림 발생 당시 작업자는 다음 경로로 조회합니다.

```text
alerts.session_id
→ production_sessions.user_id
→ users.user_id
```

---

## 4. 테이블 관계

| 부모 테이블 | 자식 테이블 | 관계 | 자식 FK |
|---|---|---|---|
| `users` | `production_sessions` | 1:N | NOT NULL |
| `production_sessions` | `product_detections` | 1:N | NOT NULL |
| `product_types` | `product_detections` | 1:N | NULL 가능 |
| `production_sessions` | `alerts` | 1:N | NULL 가능 |
| `system_components` | `alerts` | 1:N | NULL 가능 |
| `product_detections` | `alerts` | 1:N | NULL 가능 |
| `users` | `alerts` | 1:N | NULL 가능 |

---

## 5. 초기 데이터 및 더미 데이터

### 기준 데이터

- 제품 유형 2종
- 시스템 구성요소 14종

### 더미 데이터

- 관리자 계정 1개
- 작업자 계정 1개
- 완료된 생산 작업 1개
- 진행 중인 생산 작업 1개
- 분류 성공 기록 1개
- 분류 실패 기록 1개
- INFO 알림 1개
- ERROR 알림 1개

더미 사용자의 `password_hash`는 구조 확인용 문자열이며, 로그인 기능 구현 후 BCrypt 해시값으로 교체해야 합니다.

---

## 6. 이전 설계 대비 변경 사항

### 사용자

| 이전 | 현재 |
|---|---|
| `INT` 기반 PK·FK | `BIGINT`로 통일 |
| `password` | `password_hash` |
| 짧은 로그인·이름 길이 | `VARCHAR(50)`으로 확장 |
| `ENUM` 중심 | `VARCHAR + CHECK` |

### 생산 작업

| 이전 | 현재 |
|---|---|
| `production_date` 저장 | `started_at`에서 날짜 계산 |
| `chocolate_set_count` 저장 | 생산 수량과 제품 기준으로 계산 |
| 단순 상태 검증 | 목표량·수량·종료 시각 정합성 검증 추가 |

### 제품 유형

| 이전 | 현재 |
|---|---|
| 제품 코드 없음 | `product_type_code` 추가 |
| `unit_type`, `set_quantity` | `unit_per_set`으로 단순화 |
| `is_active` 포함 | 고정 제품 구조에 불필요하여 제거 |

### 제품 분류 결과

| 이전 | 현재 |
|---|---|
| `products` | `product_detections` |
| `product_id` | `product_detection_id` |
| 분류 결과 필수 | 실패 시 제품 유형·신뢰도 `NULL` 허용 |
| `DECIMAL(5,2)` | `DECIMAL(5,4)` |
| 상태값만 제한 | 성공·실패와 결과값의 정합성 검증 추가 |

### 시스템 구성요소

| 이전 | 현재 |
|---|---|
| 구성요소 이름만 사용 | `component_code` 추가 |
| `status` | `current_status` |
| `updated_at` | `status_updated_at` |
| `description` 포함 | 상세 원인은 `alerts.alert_message`에서 관리 |

### 알림

| 이전 | 현재 |
|---|---|
| 발생 사용자 `user_id` 저장 | 생산 작업을 통해 작업자 조회 |
| `product_id` | `product_detection_id` |
| `severity` | `priority` |
| `result_status` | `recovery_status` |
| `status` | `check_status` |
| `message` | `alert_message` |
| `acknowledged_by_user_id` | `checked_by_user_id` |
| `acknowledged_at` | `checked_at` |
| 복구 시각 없음 | `recovered_at` 추가 |
| 상태 간 연관 검증 부족 | 복구·확인 값과 시각의 정합성 검증 추가 |

---

## 7. 실행 방법

DBeaver에서 `smart_sorting_system.sql` 파일을 열고 전체 스크립트를 실행합니다.

```text
Alt + X
```

현재 커서가 있는 SQL 문만 실행할 때는 다음을 사용합니다.

```text
Ctrl + Enter
```

생성 확인:

```sql
USE smart_sorting_system;

SHOW TABLES;
```

예상 테이블:

```text
alerts
product_detections
product_types
production_sessions
system_components
users
```
