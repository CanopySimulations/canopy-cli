Steps:

- Generate new code using `../canopy-cli.nswag.json`.
- Remember to re-download swagger specification JSON.


There is a known issue in NSwag which necessitates manually changing the accept header for PostConfig to `application/json`: https://github.com/RicoSuter/NSwag/issues/2384
