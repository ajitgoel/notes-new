**1xx — Informational** 

| Code | Name                | Meaning                                                                    |
| ---- | ------------------- | -------------------------------------------------------------------------- |
| 100  | Continue            | Server received the request headers; client should send the body           |
| 101  | Switching Protocols | Server is switching to a different protocol (e.g., upgrading to WebSocket) |
| 102  | Processing          | Server is working on it but has no response yet (WebDAV)                   |
| 103  | Early Hints         | Server sends preliminary headers (e.g., `Link` for preloading resources)   |
**2xx — Success** 

| Code | Name            | Meaning                                                                        |
| ---- | --------------- | ------------------------------------------------------------------------------ |
| ==200==  | ==OK==              | ==Standard success. Response body contains the result==                            |
| ==201==  | ==Created==         | ==A new resource was created (return it + `Location` header)==                     |
| ==202==  | ==Accepted==        | ==Request received but processing hasn’t completed (async jobs)==                  |
| ==204==  | ==No Content==      | ==Success but nothing to return (common for DELETE)==                              |
| 206  | Partial Content | Server is returning part of a resource (range requests, e.g., video streaming) |
**3xx — Redirection** 

| Code | Name               | Meaning                                                                      |
| ---- | ------------------ | ---------------------------------------------------------------------------- |
| 301  | Moved Permanently  | Resource has a new permanent URL. Clients/search engines should update links |
| 302  | Found              | Temporary redirect (historically misused; use 303 or 307 instead)            |
| 303  | See Other          | Redirect with GET (used after POST to redirect to a result page)             |
| 304  | Not Modified       | Cached version is still valid; no need to re-download                        |
| 307  | Temporary Redirect | Like 302 but strictly preserves the HTTP method                              |
| 308  | Permanent Redirect | Like 301 but strictly preserves the HTTP method                              |
**4xx — Client Errors** 

| Code    | Name                   | Meaning                                                                                |
| ------- | ---------------------- | -------------------------------------------------------------------------------------- |
| ==400== | ==Bad Request==        | ==Malformed syntax, invalid parameters, bad JSON==                                     |
| ==401== | ==Unauthorized==       | ==Not authenticated — no valid credentials provided==                                  |
| ==403== | ==Forbidden==          | ==Authenticated but not authorized — you don’t have permission==                       |
| ==404== | ==Not Found==          | ==Resource doesn’t exist at this URL==                                                 |
| 405     | Method Not Allowed     | This endpoint doesn’t support that HTTP method (e.g., POST on a read-only resource)    |
| 406     | Not Acceptable         | Server can’t produce a response matching the `Accept` header                           |
| 408     | Request Timeout        | Client took too long to send the request                                               |
| 409     | Conflict               | Request conflicts with current state (e.g., duplicate resource, edit conflict)         |
| 410     | Gone                   | Resource existed but has been permanently deleted (stronger than 404)                  |
| 413     | Payload Too Large      | Request body exceeds server limits                                                     |
| 415     | Unsupported Media Type | Server doesn’t support the `Content-Type` sent                                         |
| 422     | Unprocessable Entity   | Syntax is fine but the data is semantically invalid (e.g., email field contains “abc”) |
| ==429== | ==Too Many Requests==  | ==Rate limit exceeded — back off and retry later==                                     |
**5xx — Server Errors**

| Code    | Name                      | Meaning                                                        |
| ------- | ------------------------- | -------------------------------------------------------------- |
| ==500== | ==Internal Server Error== | ==Generic server failure — something broke==                   |
| 501     | Not Implemented           | Server doesn’t support the requested functionality             |
| ==502==     | ==Bad Gateway==               | ==Server acting as a proxy got an invalid response from upstream== |
| ==503==     | ==Service Unavailable==       | ==Server is down or overloaded (temporary — retry later)==         |
| 504     | Gateway Timeout           | Proxy/gateway didn’t get a response from upstream in time      |
**The Ones You’ll Use 90% of the Time** 
**Happy path:** 200, 201, 204  
**Client messed up:** 400, 401, 403, 404, 409, 422, 429
**Server messed up:** 500, 502, 503