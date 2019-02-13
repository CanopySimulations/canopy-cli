Steps:

- Generate new code using `nswag.json`.
- Remember to re-download swagger specification JSON.
- Replace partial method calls to PrepareRequest to base class method calls.
 - PrepareRequest(client_, request_, urlBuilder_); -> base.PrepareRequest(client_, request_, urlBuilder_);
