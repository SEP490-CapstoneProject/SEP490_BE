Hướng dẫn nhanh - secrets/google

Các file tạo sẵn (thay giá trị REPLACE_ME_* bằng key thật của bạn):
- google_ai_api_key.txt  -> chứa Google AI API key (một dòng)
- cloudflare_api_token.txt -> chứa Cloudflare API token (một dòng)
- .env -> file env cho docker-compose (AI__Google__ApiKey, Cloudflare__ApiToken, Cloudflare__AccountId)

Lưu ý:
- KHÔNG commit thư mục ./secrets vào Git. Thư mục này đã được thêm vào .gitignore.
- Sau khi điền, khởi động lại stack: docker-compose up -d --build
- Nếu dùng Docker Swarm và Docker secrets, dùng scripts/fetch-google-cloudflare-secrets.ps1 hoặc create-docker-secret.ps1 để tạo Docker secret từ các file này.

Nếu muốn, tôi có thể tạo Docker secret tự động từ các file (yêu cầu Docker Swarm)."