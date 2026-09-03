import { Box, Stack, Typography } from "@mui/material";

export const eosLogoUrl = `${import.meta.env.BASE_URL}generated-assets/brand/eos.svg`;

export function AppBrand() {
  return (
    <Stack
      direction="row"
      spacing={1}
      sx={{ alignItems: "center", minWidth: 0 }}
    >
      <Box
        component="img"
        src={eosLogoUrl}
        alt="EOS"
        sx={{ width: 32, height: 32, flexShrink: 0 }}
        onError={(event) => {
          event.currentTarget.style.display = "none";
        }}
      />
      <Typography variant="subtitle1" sx={{ fontWeight: 750 }}>
        علم و صنعت
      </Typography>
    </Stack>
  );
}
