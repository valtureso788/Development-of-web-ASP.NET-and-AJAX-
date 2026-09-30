@echo off
echo.
echo === TEST 1: GET /doc - expect 200 OK + ETag header ===
curl -i http://localhost:5138/doc
echo.
echo.
echo === TEST 2: GET with If-None-Match - expect 304 Not Modified ===
curl -i http://localhost:5138/doc -H "If-None-Match: \"v1\""
echo.
echo.
echo === TEST 3: PUT without If-Match - expect 428 ===
curl -i -X PUT http://localhost:5138/doc -H "Content-Type: application/json" -d "{\"text\":\"No ETag attempt\"}"
echo.
echo.
echo === TEST 4: PUT with correct If-Match v1 - expect 200 OK version=2 ===
curl -i -X PUT http://localhost:5138/doc -H "Content-Type: application/json" -H "If-Match: \"v1\"" -d "{\"text\":\"Edit from A\"}"
echo.
echo.
echo === TEST 5: PUT with stale If-Match v1 - expect 412 Precondition Failed ===
curl -i -X PUT http://localhost:5138/doc -H "Content-Type: application/json" -H "If-Match: \"v1\"" -d "{\"text\":\"Edit from B\"}"
echo.
echo.
echo === TEST 6: Final GET - verify state ===
curl -i http://localhost:5138/doc
echo.
