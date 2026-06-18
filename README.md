# PharmacyBillingService - Hướng dẫn chạy và kết nối

## Mục đích
Tài liệu này hướng dẫn bạn chạy module `Pharmacy & Billing Service` từ đầu khi mở máy mới và chưa chạy gì cả.

## 1. Chuẩn bị
- Máy phải có:
  - .NET SDK 8.0
  - Docker Desktop (nếu muốn chạy Docker)
- Mạng: máy phải nằm trong cùng mạng nội bộ nếu muốn người khác truy cập từ máy khác.

## 2. Chạy bằng Docker (khuyến nghị)
### 2.1. Mở terminal vào thư mục gốc dự án
```powershell
cd "d:\season 4 ver 1\monthu3p2\PharmacyBilling"
```

### 2.2. Khởi động Docker Compose
```powershell
docker compose up --build
```

### 2.3. Kiểm tra
- Container API chạy trên port `5000`
- Container SQL Server chạy trên port `1433`

Nếu Docker Compose chạy thành công, truy cập API bằng:
- `http://localhost:5000/swagger`

Nếu muốn máy khác trong cùng mạng truy cập, dùng IP của máy bạn:
- `http://<IP-cua-ban>:5000/swagger`
  - ví dụ: `http://26.88.31.108:5000/swagger`

### 2.4. Lưu ý firewall
Nếu máy khác truy cập không được, hãy mở port `5000` cho ứng dụng trên Windows Firewall.

## 3. Chạy bằng .NET trực tiếp
Nếu không dùng Docker, bạn có thể chạy trực tiếp backend:

### 3.1. Mở terminal vào thư mục service
```powershell
cd "d:\season 4 ver 1\monthu3p2\PharmacyBilling\PharmacyBillingService"
```

### 3.2. Build và chạy
```powershell
dotnet build

dotnet run
```

### 3.3. Chạy SQL Server
Nếu không dùng Docker, bạn cần có SQL Server chạy và sửa `appsettings.json` sao cho `DefaultConnection` trỏ về SQL Server thật.

## 4. Các API chính
### 4.1. Đăng nhập
- `POST /api/Auth/login`
- Body sample:
```json
{
  "username": "admin",
  "password": "123456"
}
```
- Trả về token JWT.

### 4.2. Medicine
- `GET /api/Medicine` - xem danh sách thuốc
- `GET /api/Medicine/{id}` - xem thuốc theo id
- `POST /api/Medicine` - thêm thuốc
- `PUT /api/Medicine/{id}` - sửa thuốc
- `DELETE /api/Medicine/{id}` - xóa thuốc

### 4.3. Invoice
- `GET /api/Invoice` - xem tất cả hóa đơn (`Admin/Nurse/Doctor`)
- `GET /api/Invoice/{id}` - xem hóa đơn theo id
- `GET /api/Invoice/my` - xem hóa đơn của riêng `Patient`
- `GET /api/Invoice/patient/{name}` - tìm hóa đơn theo tên bệnh nhân (`Admin/Nurse`)
- `POST /api/Invoice` - tạo hóa đơn (phải có token)
- `PUT /api/Invoice/{id}/status` - cập nhật trạng thái hóa đơn

### 4.4. Consume prescription
- `POST /api/Invoice/consume-prescription`
- Body sample:
```json
{
  "patientName": "patient",
  "consultationFee": 50000,
  "items": [
    { "medicineId": 1, "quantity": 2 },
    { "medicineId": 2, "quantity": 1 }
  ]
}
```
- Endpoint này sẽ:
  - kiểm tra tồn kho
  - trừ thuốc
  - tạo hóa đơn với `ConsultationFee`, `MedicineFee`, `TotalAmount`

## 5. Các tài khoản mặc định
- `admin` / `123456` → vai trò `Admin`
- `doctor` / `123456` → vai trò `Doctor`
- `nurse` / `123456` → vai trò `Nurse`
- `patient` / `123456` → vai trò `Patient`

## 6. Nếu máy khác truy cập không được
1. Kiểm tra IP thật của máy bạn trong cùng mạng.
2. Dùng URL: `http://<IP-cua-ban>:5000/swagger`
3. Mở port `5000` trên firewall nếu cần.
4. Nếu dùng Docker, chắc chắn container API đang chạy.

## 7. Kết nối từ Vue/Axios
Ví dụ cấu hình Axios:
```js
import axios from 'axios';

const api = axios.create({
  baseURL: 'http://<IP-cua-ban>:5000/api',
});

export function login(username, password) {
  return api.post('/Auth/login', { username, password });
}

export function getMedicines(token) {
  return api.get('/Medicine', { headers: { Authorization: `Bearer ${token}` } });
}

export function createInvoice(invoice, token) {
  return api.post('/Invoice', invoice, { headers: { Authorization: `Bearer ${token}` } });
}
```

## 8. Ghi chú
- Nếu bạn bật máy rồi chạy Docker, chỉ cần thực hiện lại `docker compose up --build` tại thư mục gốc.
- Nếu bạn chạy .NET trực tiếp, dùng `dotnet run` trong `PharmacyBillingService`.
- Swagger sẽ giúp bạn thử API nhanh mà không cần Vue.
