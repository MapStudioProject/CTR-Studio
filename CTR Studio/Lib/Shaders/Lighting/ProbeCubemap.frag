// FS0FBE9460CCFCFAB0
#version 440 core
#extension GL_ARB_separate_shader_objects : enable
#extension GL_ARB_shader_ballot : require
#extension GL_ARB_shader_viewport_layer_array : require
#extension GL_EXT_shader_image_load_formatted : require
#extension GL_EXT_texture_shadow_lod : require
#pragma optionNV(fastmath off)

#define ftoi floatBitsToInt
#define ftou floatBitsToUint
#define itof intBitsToFloat
#define utof uintBitsToFloat

bvec2 HalfFloatNanComparison(bvec2 comparison, vec2 pair1, vec2 pair2) {
    bvec2 is_nan1 = isnan(pair1);
    bvec2 is_nan2 = isnan(pair2);
    return bvec2(comparison.x || is_nan1.x || is_nan2.x, comparison.y || is_nan1.y || is_nan2.y);
}

const float fswzadd_modifiers_a[] = float[4](-1.0f,  1.0f, -1.0f,  0.0f );
const float fswzadd_modifiers_b[] = float[4](-1.0f, -1.0f,  1.0f, -1.0f );

layout (location = 0) out vec4 frag_color0;
layout (location = 1) out vec4 frag_color1;
layout (location = 2) out vec4 frag_color2;
layout (location = 3) out vec4 frag_color3;
layout (location = 4) out vec4 frag_color4;
layout (location = 5) out vec4 frag_color5;
layout (location = 6) out vec4 frag_color6;
layout (location = 7) out vec4 frag_color7;
layout (location = 0) smooth in vec4 in_attr0;

layout (binding = 128) uniform sampler2D sampler0;
layout (binding = 129) uniform samplerCube sampler1;

layout (std140, binding = 57) uniform cbuf_block3 {
    uvec4 cbuf3[5];
};

layout (std140, binding = 58) uniform cbuf_block4 {
    uvec4 cbuf4[7];
};

float gpr0 = 0.0f;
float gpr1 = 0.0f;
float gpr2 = 0.0f;
float gpr3 = 0.0f;
float gpr4 = 0.0f;
float gpr5 = 0.0f;
float gpr6 = 0.0f;
float gpr7 = 0.0f;
float gpr8 = 0.0f;
float gpr9 = 0.0f;
float gpr10 = 0.0f;
float gpr11 = 0.0f;
float gpr12 = 0.0f;
float gpr13 = 0.0f;
float gpr14 = 0.0f;
float gpr15 = 0.0f;
float gpr16 = 0.0f;
float gpr17 = 0.0f;
float gpr18 = 0.0f;
float gpr19 = 0.0f;
float gpr20 = 0.0f;
float gpr21 = 0.0f;
float gpr22 = 0.0f;
float gpr23 = 0.0f;
float gpr24 = 0.0f;
float gpr25 = 0.0f;
float gpr26 = 0.0f;
float gpr27 = 0.0f;
float gpr28 = 0.0f;
float gpr29 = 0.0f;
float gpr30 = 0.0f;
float gpr31 = 0.0f;
float gpr32 = 0.0f;
float gpr33 = 0.0f;
float gpr34 = 0.0f;
float gpr35 = 0.0f;
float gpr36 = 0.0f;
float gpr37 = 0.0f;
float gpr38 = 0.0f;
float gpr39 = 0.0f;
float gpr40 = 0.0f;
float gpr41 = 0.0f;
float gpr42 = 0.0f;
float gpr43 = 0.0f;
float gpr44 = 0.0f;
float gpr45 = 0.0f;
float gpr46 = 0.0f;
float gpr256 = 0.0f;
float gpr257 = 0.0f;
float gpr258 = 0.0f;

bool zero_flag = false;
bool sign_flag = false;
bool carry_flag = false;
bool overflow_flag = false;

void main() {
    uint jmp_to = 10U;
    while (true) {
        switch (jmp_to) {
        case 0xAU: {
            // 00008 IPA (0xe003ff87cff7ff08)
            gpr8 = gl_FragCoord.w;
            // 00010 MUFU (0x5080000000470808)
            precise float tmp1 = (utof(0x3F800000U) / gpr8);
            gpr8 = tmp1;
            // 00018 IPA (0xe043ff880087ff00)
            gpr0 = in_attr0.x;
            // 00028 IPA (0xe043ff884087ff01)
            gpr1 = in_attr0.y;
            // 00030 TEXS (0xd8200082c0170002)
            gpr256 = texture(sampler0, vec2(gpr0, gpr1)).x;
            gpr257 = texture(sampler0, vec2(gpr0, gpr1)).y;
            gpr258 = texture(sampler0, vec2(gpr0, gpr1)).z;
            gpr2 = gpr256;
            gpr3 = gpr257;
            gpr44 = gpr258;
            // 00038 FMUL_R (0x5c69100002c70304)
            float tmp2 = -(gpr44);
            precise float tmp3 = (gpr3 * tmp2);
            gpr4 = tmp3;
            // 00048 FMUL_R (0x5c69100000370205)
            float tmp4 = -(gpr3);
            precise float tmp5 = (gpr2 * tmp4);
            gpr5 = tmp5;
            // 00050 FMUL_C (0x4c68101001470406)
            precise float tmp6 = (gpr4 * utof(cbuf4[5][0]));
            gpr6 = tmp6;
            // 00058 FMUL_C (0x4c68101000c70407)
            precise float tmp7 = (gpr4 * utof(cbuf4[3][0]));
            gpr7 = tmp7;
            // 00068 FMUL_C (0x4c68101001070404)
            precise float tmp8 = (gpr4 * utof(cbuf4[4][0]));
            gpr4 = tmp8;
            // 00070 FMUL_R (0x5c69100002c70201)
            float tmp9 = -(gpr44);
            precise float tmp10 = (gpr2 * tmp9);
            gpr1 = tmp10;
            // 00078 FFMA_CR (0x49a003100157050b)
            precise float tmp11 = fma(gpr5, utof(cbuf4[5][1]), gpr6);
            gpr11 = tmp11;
            // 00088 FFMA_CR (0x49a002100117050c)
            precise float tmp12 = fma(gpr5, utof(cbuf4[4][1]), gpr4);
            gpr12 = tmp12;
            // 00090 FMUL_R (0x5c69100000370204)
            float tmp13 = -(gpr3);
            precise float tmp14 = (gpr2 * tmp13);
            gpr4 = tmp14;
            // 00098 FMUL_R (0x5c68100000370206)
            precise float tmp15 = (gpr2 * gpr3);
            gpr6 = tmp15;
            // 000a8 FMUL_R (0x5c68100002c7030a)
            precise float tmp16 = (gpr3 * gpr44);
            gpr10 = tmp16;
            // 000b0 FMUL_R (0x5c68100002c70209)
            precise float tmp17 = (gpr2 * gpr44);
            gpr9 = tmp17;
            // 000b8 FFMA_CR (0x49a0039000d7050d)
            precise float tmp18 = fma(gpr5, utof(cbuf4[3][1]), gpr7);
            gpr13 = tmp18;
            // 000c8 FMUL_R (0x5c69100002c7030e)
            float tmp19 = -(gpr44);
            precise float tmp20 = (gpr3 * tmp19);
            gpr14 = tmp20;
            // 000d0 FMUL_C (0x4c6810100147040f)
            precise float tmp21 = (gpr4 * utof(cbuf4[5][0]));
            gpr15 = tmp21;
            // 000d8 FMUL_C (0x4c68101000c70411)
            precise float tmp22 = (gpr4 * utof(cbuf4[3][0]));
            gpr17 = tmp22;
            // 000e8 FMUL_C (0x4c68101001070414)
            precise float tmp23 = (gpr4 * utof(cbuf4[4][0]));
            gpr20 = tmp23;
            // 000f0 FMUL_C (0x4c68101001470604)
            precise float tmp24 = (gpr6 * utof(cbuf4[5][0]));
            gpr4 = tmp24;
            // 000f8 FMUL_C (0x4c68101001070605)
            precise float tmp25 = (gpr6 * utof(cbuf4[4][0]));
            gpr5 = tmp25;
            // 00108 FMUL_C (0x4c68101000c70608)
            precise float tmp26 = (gpr6 * utof(cbuf4[3][0]));
            gpr8 = tmp26;
            // 00110 FMUL_C (0x4c6810100147011c)
            precise float tmp27 = (gpr1 * utof(cbuf4[5][0]));
            gpr28 = tmp27;
            // 00118 FMUL_C (0x4c68101001070118)
            precise float tmp28 = (gpr1 * utof(cbuf4[4][0]));
            gpr24 = tmp28;
            // 00128 FMUL_C (0x4c68101000c70115)
            precise float tmp29 = (gpr1 * utof(cbuf4[3][0]));
            gpr21 = tmp29;
            // 00130 FMUL_C (0x4c6810100147091d)
            precise float tmp30 = (gpr9 * utof(cbuf4[5][0]));
            gpr29 = tmp30;
            // 00138 FMUL_C (0x4c68101000c7091f)
            precise float tmp31 = (gpr9 * utof(cbuf4[3][0]));
            gpr31 = tmp31;
            // 00148 FMUL_C (0x4c68101001070a07)
            precise float tmp32 = (gpr10 * utof(cbuf4[4][0]));
            gpr7 = tmp32;
            // 00150 FMUL_R (0x5c68100002c72c00)
            precise float tmp33 = (gpr44 * gpr44);
            gpr0 = tmp33;
            // 00158 FMUL_C (0x4c68101001070920)
            precise float tmp34 = (gpr9 * utof(cbuf4[4][0]));
            gpr32 = tmp34;
            // 00168 FFMA_CR (0x49a0021001570e13)
            precise float tmp35 = fma(gpr14, utof(cbuf4[5][1]), gpr4);
            gpr19 = tmp35;
            // 00170 FFMA_CR (0x49a0029001170e16)
            precise float tmp36 = fma(gpr14, utof(cbuf4[4][1]), gpr5);
            gpr22 = tmp36;
            // 00178 FFMA_CR (0x49a00a1001170a1b)
            precise float tmp37 = fma(gpr10, utof(cbuf4[4][1]), gpr20);
            gpr27 = tmp37;
            // 00188 FMUL_R (0x5c6810000037032b)
            precise float tmp38 = (gpr3 * gpr3);
            gpr43 = tmp38;
            // 00190 FFMA_CR (0x49a0041000d70e17)
            precise float tmp39 = fma(gpr14, utof(cbuf4[3][1]), gpr8);
            gpr23 = tmp39;
            // 00198 FFMA_CR (0x49a0089000d70a1a)
            precise float tmp40 = fma(gpr10, utof(cbuf4[3][1]), gpr17);
            gpr26 = tmp40;
            // 001a8 FFMA_CR (0x49a00e1001570614)
            precise float tmp41 = fma(gpr6, utof(cbuf4[5][1]), gpr28);
            gpr20 = tmp41;
            // 001b0 FFMA_CR (0x49a0079001570a19)
            precise float tmp42 = fma(gpr10, utof(cbuf4[5][1]), gpr15);
            gpr25 = tmp42;
            // 001b8 FFMA_CR (0x49a00c1001170608)
            precise float tmp43 = fma(gpr6, utof(cbuf4[4][1]), gpr24);
            gpr8 = tmp43;
            // 001c8 FFMA_CR (0x49a00a9000d70612)
            precise float tmp44 = fma(gpr6, utof(cbuf4[3][1]), gpr21);
            gpr18 = tmp44;
            // 001d0 FFMA_CR (0x49a00e9001570615)
            precise float tmp45 = fma(gpr6, utof(cbuf4[5][1]), gpr29);
            gpr21 = tmp45;
            // 001d8 FFMA_CR (0x49a00f9000d70605)
            precise float tmp46 = fma(gpr6, utof(cbuf4[3][1]), gpr31);
            gpr5 = tmp46;
            // 001e8 FFMA_CR (0x49a0039001170611)
            precise float tmp47 = fma(gpr6, utof(cbuf4[4][1]), gpr7);
            gpr17 = tmp47;
            // 001f0 FMUL_C (0x4c68101001470a21)
            precise float tmp48 = (gpr10 * utof(cbuf4[5][0]));
            gpr33 = tmp48;
            // 001f8 FMUL_C (0x4c68101000c70a10)
            precise float tmp49 = (gpr10 * utof(cbuf4[3][0]));
            gpr16 = tmp49;
            // 00208 FFMA_CR (0x49a0101001170604)
            precise float tmp50 = fma(gpr6, utof(cbuf4[4][1]), gpr32);
            gpr4 = tmp50;
            // 00210 FFMA_CR (0x49a0099001670007)
            precise float tmp51 = fma(gpr0, utof(cbuf4[5][2]), gpr19);
            gpr7 = tmp51;
            // 00218 FFMA_CR (0x49a00b1001270013)
            precise float tmp52 = fma(gpr0, utof(cbuf4[4][2]), gpr22);
            gpr19 = tmp52;
            // 00228 FFMA_CR (0x49a00b9000e70016)
            precise float tmp53 = fma(gpr0, utof(cbuf4[3][2]), gpr23);
            gpr22 = tmp53;
            // 00230 FFMA_CR (0x49a00d1000e70018)
            precise float tmp54 = fma(gpr0, utof(cbuf4[3][2]), gpr26);
            gpr24 = tmp54;
            // 00238 FFMA_CR (0x49a00a1001672b14)
            precise float tmp55 = fma(gpr43, utof(cbuf4[5][2]), gpr20);
            gpr20 = tmp55;
            // 00248 FFMA_CR (0x49a00c9001670017)
            precise float tmp56 = fma(gpr0, utof(cbuf4[5][2]), gpr25);
            gpr23 = tmp56;
            // 00250 FFMA_CR (0x49a00d900127001a)
            precise float tmp57 = fma(gpr0, utof(cbuf4[4][2]), gpr27);
            gpr26 = tmp57;
            // 00258 FFMA_CR (0x49a0041001272b1b)
            precise float tmp58 = fma(gpr43, utof(cbuf4[4][2]), gpr8);
            gpr27 = tmp58;
            // 00268 FFMA_CR (0x49a0091000e72b19)
            precise float tmp59 = fma(gpr43, utof(cbuf4[3][2]), gpr18);
            gpr25 = tmp59;
            // 00270 FFMA_CR (0x49a00a9001672b1c)
            precise float tmp60 = fma(gpr43, utof(cbuf4[5][2]), gpr21);
            gpr28 = tmp60;
            // 00278 FFMA_CR (0x49a0029000e72b1d)
            precise float tmp61 = fma(gpr43, utof(cbuf4[3][2]), gpr5);
            gpr29 = tmp61;
            // 00288 FFMA_CR (0x49a010900157060f)
            precise float tmp62 = fma(gpr6, utof(cbuf4[5][1]), gpr33);
            gpr15 = tmp62;
            // 00290 FFMA_CR (0x49a0081000d70610)
            precise float tmp63 = fma(gpr6, utof(cbuf4[3][1]), gpr16);
            gpr16 = tmp63;
            // 00298 FFMA_CR (0x49a0021001272b1f)
            precise float tmp64 = fma(gpr43, utof(cbuf4[4][2]), gpr4);
            gpr31 = tmp64;
            // 002a8 FMUL_R (0x5c68100000270206)
            precise float tmp65 = (gpr2 * gpr2);
            gpr6 = tmp65;
            // 002b0 FFMA_CR (0x49a00a1001770e15)
            precise float tmp66 = fma(gpr14, utof(cbuf4[5][3]), gpr20);
            gpr21 = tmp66;
            // 002b8 FFMA_CR (0x49a0099001370108)
            precise float tmp67 = fma(gpr1, utof(cbuf4[4][3]), gpr19);
            gpr8 = tmp67;
            // 002c8 FFMA_CR (0x49a00b1000f70105)
            precise float tmp68 = fma(gpr1, utof(cbuf4[3][3]), gpr22);
            gpr5 = tmp68;
            // 002d0 FMUL_C (0x4c69101000472c14)
            float tmp69 = -(utof(cbuf4[1][0]));
            precise float tmp70 = (gpr44 * tmp69);
            gpr20 = tmp70;
            // 002d8 FFMA_CR (0x49a00d9001370e16)
            precise float tmp71 = fma(gpr14, utof(cbuf4[4][3]), gpr27);
            gpr22 = tmp71;
            // 002e8 FFMA_CR (0x49a00c9000f70e13)
            precise float tmp72 = fma(gpr14, utof(cbuf4[3][3]), gpr25);
            gpr19 = tmp72;
            // 002f0 FFMA_CR (0x49a00c1000f70104)
            precise float tmp73 = fma(gpr1, utof(cbuf4[3][3]), gpr24);
            gpr4 = tmp73;
            // 002f8 FFMA_CR (0x49a00e1001770a12)
            precise float tmp74 = fma(gpr10, utof(cbuf4[5][3]), gpr28);
            gpr18 = tmp74;
            // 00308 FFMA_CR (0x49a00e9000f70a0e)
            precise float tmp75 = fma(gpr10, utof(cbuf4[3][3]), gpr29);
            gpr14 = tmp75;
            // 00310 FFMA_CR (0x49a00b9001770100)
            precise float tmp76 = fma(gpr1, utof(cbuf4[5][3]), gpr23);
            gpr0 = tmp76;
            // 00318 FFMA_CR (0x49a00f9001370a0a)
            precise float tmp77 = fma(gpr10, utof(cbuf4[4][3]), gpr31);
            gpr10 = tmp77;
            // 00328 FMUL_C (0x4c69101000072c18)
            float tmp78 = -(utof(cbuf4[0][0]));
            precise float tmp79 = (gpr44 * tmp78);
            gpr24 = tmp79;
            // 00330 FFMA_CR (0x49a0059001670617)
            precise float tmp80 = fma(gpr6, utof(cbuf4[5][2]), gpr11);
            gpr23 = tmp80;
            // 00338 FFMA_CR (0x49a006100127061c)
            precise float tmp81 = fma(gpr6, utof(cbuf4[4][2]), gpr12);
            gpr28 = tmp81;
            // 00348 FFMA_CR (0x49a0069000e7061b)
            precise float tmp82 = fma(gpr6, utof(cbuf4[3][2]), gpr13);
            gpr27 = tmp82;
            // 00350 FFMA_CR (0x49a007900167061e)
            precise float tmp83 = fma(gpr6, utof(cbuf4[5][2]), gpr15);
            gpr30 = tmp83;
            // 00358 FFMA_CR (0x49a0081000e7061f)
            precise float tmp84 = fma(gpr6, utof(cbuf4[3][2]), gpr16);
            gpr31 = tmp84;
            // 00368 FFMA_CR (0x49a0089001270620)
            precise float tmp85 = fma(gpr6, utof(cbuf4[4][2]), gpr17);
            gpr32 = tmp85;
            // 00370 FMUL_C (0x4c69101000872c19)
            float tmp86 = -(utof(cbuf4[2][0]));
            precise float tmp87 = (gpr44 * tmp86);
            gpr25 = tmp87;
            // 00378 FFMA_CR (0x49a00a100057020d)
            precise float tmp88 = fma(gpr2, utof(cbuf4[1][1]), gpr20);
            gpr13 = tmp88;
            // 00388 FFMA_CR (0x49a00c100017020c)
            precise float tmp89 = fma(gpr2, utof(cbuf4[0][1]), gpr24);
            gpr12 = tmp89;
            // 00390 FFMA_CR (0x49a00e1001370911)
            precise float tmp90 = fma(gpr9, utof(cbuf4[4][3]), gpr28);
            gpr17 = tmp90;
            // 00398 FFMA_CR (0x49a00b9001770910)
            precise float tmp91 = fma(gpr9, utof(cbuf4[5][3]), gpr23);
            gpr16 = tmp91;
            // 003a8 FFMA_CR (0x49a00d9000f7090f)
            precise float tmp92 = fma(gpr9, utof(cbuf4[3][3]), gpr27);
            gpr15 = tmp92;
            // 003b0 FFMA_CR (0x49a00f1001770929)
            precise float tmp93 = fma(gpr9, utof(cbuf4[5][3]), gpr30);
            gpr41 = tmp93;
            // 003b8 FFMA_CR (0x49a00f9000f70928)
            precise float tmp94 = fma(gpr9, utof(cbuf4[3][3]), gpr31);
            gpr40 = tmp94;
            // 003c8 FFMA_CR (0x49a0101001370927)
            precise float tmp95 = fma(gpr9, utof(cbuf4[4][3]), gpr32);
            gpr39 = tmp95;
            // 003d0 FFMA_CR (0x49a00c9000970309)
            precise float tmp96 = fma(gpr3, utof(cbuf4[2][1]), gpr25);
            gpr9 = tmp96;
            // 003d8 FFMA_CR (0x49a00a100057030b)
            precise float tmp97 = fma(gpr3, utof(cbuf4[1][1]), gpr20);
            gpr11 = tmp97;
            // 003e8 FFMA_CR (0x49a006900067030d)
            precise float tmp98 = fma(gpr3, utof(cbuf4[1][2]), gpr13);
            gpr13 = tmp98;
            // 003f0 FFMA_CR (0x49a0039001770107)
            precise float tmp99 = fma(gpr1, utof(cbuf4[5][3]), gpr7);
            gpr7 = tmp99;
            // 003f8 FFMA_CR (0x49a00d1001370101)
            precise float tmp100 = fma(gpr1, utof(cbuf4[4][3]), gpr26);
            gpr1 = tmp100;
            // 00408 FFMA_CR (0x49a006100027031b)
            precise float tmp101 = fma(gpr3, utof(cbuf4[0][2]), gpr12);
            gpr27 = tmp101;
            // 00410 FFMA_CR (0x49a00c900097021a)
            precise float tmp102 = fma(gpr2, utof(cbuf4[2][1]), gpr25);
            gpr26 = tmp102;
            // 00418 FFMA_CR (0x49a10c900097020c)
            float tmp103 = -(utof(cbuf4[2][1]));
            precise float tmp104 = fma(gpr2, tmp103, gpr25);
            gpr12 = tmp104;
            // 00428 FFMA_CR (0x49a00c1000170317)
            precise float tmp105 = fma(gpr3, utof(cbuf4[0][1]), gpr24);
            gpr23 = tmp105;
            // 00430 FFMA_CR (0x49a1049000a70219)
            float tmp106 = -(utof(cbuf4[2][2]));
            precise float tmp107 = fma(gpr2, tmp106, gpr9);
            gpr25 = tmp107;
            // 00438 FFMA_CR (0x49a105900067021c)
            float tmp108 = -(utof(cbuf4[1][2]));
            precise float tmp109 = fma(gpr2, tmp108, gpr11);
            gpr28 = tmp109;
            // 00448 FADD_C (0x4c58101000770d09)
            precise float tmp110 = (gpr13 + utof(cbuf4[1][3]));
            gpr9 = tmp110;
            // 00450 FFMA_CR (0x49a00d1000a7031a)
            precise float tmp111 = fma(gpr3, utof(cbuf4[2][2]), gpr26);
            gpr26 = tmp111;
            // 00458 FADD_C (0x4c58101000371b0d)
            precise float tmp112 = (gpr27 + utof(cbuf4[0][3]));
            gpr13 = tmp112;
            // 00468 FFMA_CR (0x49a10c1000170218)
            float tmp113 = -(utof(cbuf4[0][1]));
            precise float tmp114 = fma(gpr2, tmp113, gpr24);
            gpr24 = tmp114;
            // 00470 FFMA_CR (0x49a10b900027021d)
            float tmp115 = -(utof(cbuf4[0][2]));
            precise float tmp116 = fma(gpr2, tmp115, gpr23);
            gpr29 = tmp116;
            // 00478 FFMA_CR (0x49a1061000a7031b)
            float tmp117 = -(utof(cbuf4[2][2]));
            precise float tmp118 = fma(gpr3, tmp117, gpr12);
            gpr27 = tmp118;
            // 00488 FADD_C (0x4c58101000771c17)
            precise float tmp119 = (gpr28 + utof(cbuf4[1][3]));
            gpr23 = tmp119;
            // 00490 FADD_R (0x5c5810000167090c)
            precise float tmp120 = (gpr9 + gpr22);
            gpr12 = tmp120;
            // 00498 FADD_C (0x4c58101000b71919)
            precise float tmp121 = (gpr25 + utof(cbuf4[2][3]));
            gpr25 = tmp121;
            // 004a8 FMUL_C (0x4c68101000470209)
            precise float tmp122 = (gpr2 * utof(cbuf4[1][0]));
            gpr9 = tmp122;
            // 004b0 FADD_C (0x4c58101000b71a0b)
            precise float tmp123 = (gpr26 + utof(cbuf4[2][3]));
            gpr11 = tmp123;
            // 004b8 FFMA_CR (0x49a10c100027031a)
            float tmp124 = -(utof(cbuf4[0][2]));
            precise float tmp125 = fma(gpr3, tmp124, gpr24);
            gpr26 = tmp125;
            // 004c8 FADD_C (0x4c58101000371d18)
            precise float tmp126 = (gpr29 + utof(cbuf4[0][3]));
            gpr24 = tmp126;
            // 004d0 FADD_R (0x5c58100001370d0d)
            precise float tmp127 = (gpr13 + gpr19);
            gpr13 = tmp127;
            // 004d8 FADD_R (0x5c58100001171717)
            precise float tmp128 = (gpr23 + gpr17);
            gpr23 = tmp128;
            // 004e8 FADD_C (0x4c58101000b71b11)
            precise float tmp129 = (gpr27 + utof(cbuf4[2][3]));
            gpr17 = tmp129;
            // 004f0 FADD_R (0x5c58100001071913)
            precise float tmp130 = (gpr25 + gpr16);
            gpr19 = tmp130;
            // 004f8 FFMA_CR (0x49a10a1000570214)
            float tmp131 = -(utof(cbuf4[1][1]));
            precise float tmp132 = fma(gpr2, tmp131, gpr20);
            gpr20 = tmp132;
            // 00508 FFMA_CR (0x49a0049000570309)
            precise float tmp133 = fma(gpr3, utof(cbuf4[1][1]), gpr9);
            gpr9 = tmp133;
            // 00510 FMUL_C (0x4c68101000870210)
            precise float tmp134 = (gpr2 * utof(cbuf4[2][0]));
            gpr16 = tmp134;
            // 00518 FADD_R (0x5c58100000f7181f)
            precise float tmp135 = (gpr24 + gpr15);
            gpr31 = tmp135;
            // 00528 FADD_R (0x5c5810000127110f)
            precise float tmp136 = (gpr17 + gpr18);
            gpr15 = tmp136;
            // 00530 FADD_R (0x5c58100001570b0b)
            precise float tmp137 = (gpr11 + gpr21);
            gpr11 = tmp137;
            // 00538 FFMA_CR (0x49a10a1000670314)
            float tmp138 = -(utof(cbuf4[1][2]));
            precise float tmp139 = fma(gpr3, tmp138, gpr20);
            gpr20 = tmp139;
            // 00548 FFMA_CR (0x49a1049000672c09)
            float tmp140 = -(utof(cbuf4[1][2]));
            precise float tmp141 = fma(gpr44, tmp140, gpr9);
            gpr9 = tmp141;
            // 00550 FFMA_CR (0x49a0081000970311)
            precise float tmp142 = fma(gpr3, utof(cbuf4[2][1]), gpr16);
            gpr17 = tmp142;
            // 00558 FADD_C (0x4c58101000371a15)
            precise float tmp143 = (gpr26 + utof(cbuf4[0][3]));
            gpr21 = tmp143;
            // 00568 FMUL_C (0x4c68101000070212)
            precise float tmp144 = (gpr2 * utof(cbuf4[0][0]));
            gpr18 = tmp144;
            // 00570 FMUL_C (0x4c69101000470210)
            float tmp145 = -(utof(cbuf4[1][0]));
            precise float tmp146 = (gpr2 * tmp145);
            gpr16 = tmp146;
            // 00578 FADD_C (0x4c58101000771418)
            precise float tmp147 = (gpr20 + utof(cbuf4[1][3]));
            gpr24 = tmp147;
            // 00588 FADD_C (0x4c58101000770909)
            precise float tmp148 = (gpr9 + utof(cbuf4[1][3]));
            gpr9 = tmp148;
            // 00590 FFMA_CR (0x49a1089000a72c16)
            float tmp149 = -(utof(cbuf4[2][2]));
            precise float tmp150 = fma(gpr44, tmp149, gpr17);
            gpr22 = tmp150;
            // 00598 FMUL_C (0x4c69101000870214)
            float tmp151 = -(utof(cbuf4[2][0]));
            precise float tmp152 = (gpr2 * tmp151);
            gpr20 = tmp152;
            // 005a8 FADD_R (0x5c58100000e7150e)
            precise float tmp153 = (gpr21 + gpr14);
            gpr14 = tmp153;
            // 005b0 FFMA_CR (0x49a0091000170312)
            precise float tmp154 = fma(gpr3, utof(cbuf4[0][1]), gpr18);
            gpr18 = tmp154;
            // 005b8 FFMA_CR (0x49a0081000570311)
            precise float tmp155 = fma(gpr3, utof(cbuf4[1][1]), gpr16);
            gpr17 = tmp155;
            // 005c8 FMUL_C (0x4c69101000070215)
            float tmp156 = -(utof(cbuf4[0][0]));
            precise float tmp157 = (gpr2 * tmp156);
            gpr21 = tmp157;
            // 005d0 FADD_R (0x5c58100000870908)
            precise float tmp158 = (gpr9 + gpr8);
            gpr8 = tmp158;
            // 005d8 FADD_C (0x4c58101000b71610)
            precise float tmp159 = (gpr22 + utof(cbuf4[2][3]));
            gpr16 = tmp159;
            // 005e8 FFMA_CR (0x49a00a1000970309)
            precise float tmp160 = fma(gpr3, utof(cbuf4[2][1]), gpr20);
            gpr9 = tmp160;
            // 005f0 FFMA_CR (0x49a1091000272c12)
            float tmp161 = -(utof(cbuf4[0][2]));
            precise float tmp162 = fma(gpr44, tmp161, gpr18);
            gpr18 = tmp162;
            // 005f8 FFMA_CR (0x49a0089000672c14)
            precise float tmp163 = fma(gpr44, utof(cbuf4[1][2]), gpr17);
            gpr20 = tmp163;
            // 00608 FFMA_CR (0x49a00a9000170315)
            precise float tmp164 = fma(gpr3, utof(cbuf4[0][1]), gpr21);
            gpr21 = tmp164;
            // 00610 FMNMX_R (0x5c62578000370216)
            gpr22 = utof((!(true) ? ftou(min(abs(gpr2), abs(gpr3))) : ftou(max(abs(gpr2), abs(gpr3)))));
            // 00618 FADD_R (0x5c58100000771007)
            precise float tmp165 = (gpr16 + gpr7);
            gpr7 = tmp165;
            // 00628 FMNMX_R (0x5c62578002c70319)
            gpr25 = utof((!(true) ? ftou(min(abs(gpr3), abs(gpr44))) : ftou(max(abs(gpr3), abs(gpr44)))));
            // 00630 FADD_C (0x4c58101000371212)
            precise float tmp166 = (gpr18 + utof(cbuf4[0][3]));
            gpr18 = tmp166;
            // 00638 FADD_C (0x4c58101000771410)
            precise float tmp167 = (gpr20 + utof(cbuf4[1][3]));
            gpr16 = tmp167;
            // 00648 FFMA_CR (0x49a00a9000272c15)
            precise float tmp168 = fma(gpr44, utof(cbuf4[0][2]), gpr21);
            gpr21 = tmp168;
            // 00650 FFMA_CR (0x49a0049000a72c1a)
            precise float tmp169 = fma(gpr44, utof(cbuf4[2][2]), gpr9);
            gpr26 = tmp169;
            // 00658 FMNMX_R (0x5c60578001672c09)
            gpr9 = utof((!(true) ? ftou(min(abs(gpr44), gpr22)) : ftou(max(abs(gpr44), gpr22))));
            // 00668 FMNMX_R (0x5c6057800197021d)
            gpr29 = utof((!(true) ? ftou(min(abs(gpr2), gpr25)) : ftou(max(abs(gpr2), gpr25))));
            // 00670 MUFU (0x5080000000470909)
            precise float tmp170 = (utof(0x3F800000U) / gpr9);
            gpr9 = tmp170;
            // 00678 FADD_R (0x5c58100000571211)
            precise float tmp171 = (gpr18 + gpr5);
            gpr17 = tmp171;
            // 00688 FADD_R (0x5c58100000171010)
            precise float tmp172 = (gpr16 + gpr1);
            gpr16 = tmp172;
            // 00690 MUFU (0x5080000000471d01)
            precise float tmp173 = (utof(0x3F800000U) / gpr29);
            gpr1 = tmp173;
            // 00698 FADD_C (0x4c58101000371515)
            precise float tmp174 = (gpr21 + utof(cbuf4[0][3]));
            gpr21 = tmp174;
            // 006a8 FADD_C (0x4c58101000b71a12)
            precise float tmp175 = (gpr26 + utof(cbuf4[2][3]));
            gpr18 = tmp175;
            // 006b0 FFMA_RR (0x59a2030002c72c14)
            float tmp176 = -(gpr6);
            precise float tmp177 = fma(gpr44, gpr44, tmp176);
            gpr20 = tmp177;
            // 006b8 FFMA_RR (0x59a2158000270206)
            float tmp178 = -(gpr43);
            precise float tmp179 = fma(gpr2, gpr2, tmp178);
            gpr6 = tmp179;
            // 006c8 FADD_R (0x5c58100000471504)
            precise float tmp180 = (gpr21 + gpr4);
            gpr4 = tmp180;
            // 006d0 FADD_R (0x5c58100000071200)
            precise float tmp181 = (gpr18 + gpr0);
            gpr0 = tmp181;
            // 006d8 FFMA_CR (0x49a0061001971416)
            precise float tmp182 = fma(gpr20, utof(cbuf4[6][1]), gpr12);
            gpr22 = tmp182;
            // 006e8 FFMA_CR (0x49a007100187141b)
            precise float tmp183 = fma(gpr20, utof(cbuf4[6][0]), gpr14);
            gpr27 = tmp183;
            // 006f0 FFMA_CR (0x49a0079001a7141c)
            precise float tmp184 = fma(gpr20, utof(cbuf4[6][2]), gpr15);
            gpr28 = tmp184;
            // 006f8 FFMA_CR (0x49a008900187062a)
            precise float tmp185 = fma(gpr6, utof(cbuf4[6][0]), gpr17);
            gpr42 = tmp185;
            // 00708 FFMA_CR (0x49a0041001970625)
            precise float tmp186 = fma(gpr6, utof(cbuf4[6][1]), gpr8);
            gpr37 = tmp186;
            // 00710 FFMA_CR (0x49a0039001a70624)
            precise float tmp187 = fma(gpr6, utof(cbuf4[6][2]), gpr7);
            gpr36 = tmp187;
            // 00718 FFMA_CR (0x49a0021001870623)
            precise float tmp188 = fma(gpr6, utof(cbuf4[6][0]), gpr4);
            gpr35 = tmp188;
            // 00728 FFMA_CR (0x49a0081001970622)
            precise float tmp189 = fma(gpr6, utof(cbuf4[6][1]), gpr16);
            gpr34 = tmp189;
            // 00730 FFMA_CR (0x49a0001001a7061d)
            precise float tmp190 = fma(gpr6, utof(cbuf4[6][2]), gpr0);
            gpr29 = tmp190;
            // 00738 FMUL_R (0x5c68100000972c2e)
            precise float tmp191 = (gpr44 * gpr9);
            gpr46 = tmp191;
            // 00748 FMUL_R (0x5c68100000970321)
            precise float tmp192 = (gpr3 * gpr9);
            gpr33 = tmp192;
            // 00750 FMUL_R (0x5c68100000970220)
            precise float tmp193 = (gpr2 * gpr9);
            gpr32 = tmp193;
            // 00758 FMUL_R (0x5c6810000017030f)
            precise float tmp194 = (gpr3 * gpr1);
            gpr15 = tmp194;
            // 00768 FMUL_R (0x5c68100000170210)
            precise float tmp195 = (gpr2 * gpr1);
            gpr16 = tmp195;
            // 00770 FMUL_R (0x5c6910000017020c)
            float tmp196 = -(gpr1);
            precise float tmp197 = (gpr2 * tmp196);
            gpr12 = tmp197;
            // 00778 FMUL_R (0x5c69100000172c0e)
            float tmp198 = -(gpr1);
            precise float tmp199 = (gpr44 * tmp198);
            gpr14 = tmp199;
            // 00788 FMUL_R (0x5c68100000172c06)
            precise float tmp200 = (gpr44 * gpr1);
            gpr6 = tmp200;
            // 00790 TEXS (0xd98200a202e72000)
            gpr256 = texture(sampler1, vec3(gpr32, gpr33, gpr46)).x;
            gpr257 = texture(sampler1, vec3(gpr32, gpr33, gpr46)).y;
            gpr258 = texture(sampler1, vec3(gpr32, gpr33, gpr46)).z;
            gpr0 = gpr256;
            gpr1 = gpr257;
            gpr32 = gpr258;
            // 00798 FADD_R (0x5c58100000a7180a)
            precise float tmp201 = (gpr24 + gpr10);
            gpr10 = tmp201;
            // 007a8 FMNMX_R (0x5c62578002c70218)
            gpr24 = utof((!(true) ? ftou(min(abs(gpr2), abs(gpr44))) : ftou(max(abs(gpr2), abs(gpr44)))));
            // 007b0 FMNMX_R (0x5c60578001870305)
            gpr5 = utof((!(true) ? ftou(min(abs(gpr3), gpr24)) : ftou(max(abs(gpr3), gpr24))));
            // 007b8 MUFU (0x5080000000470505)
            precise float tmp202 = (utof(0x3F800000U) / gpr5);
            gpr5 = tmp202;
            // 007c8 FFMA_CR (0x49a0069001871418)
            precise float tmp203 = fma(gpr20, utof(cbuf4[6][0]), gpr13);
            gpr24 = tmp203;
            // 007d0 FFMA_CR (0x49a0059001a7141a)
            precise float tmp204 = fma(gpr20, utof(cbuf4[6][2]), gpr11);
            gpr26 = tmp204;
            // 007d8 FFMA_CR (0x49a0051001971419)
            precise float tmp205 = fma(gpr20, utof(cbuf4[6][1]), gpr10);
            gpr25 = tmp205;
            // 007e8 FMUL_R (0x5c69100000972c1e)
            float tmp206 = -(gpr9);
            precise float tmp207 = (gpr44 * tmp206);
            gpr30 = tmp207;
            // 007f0 FMUL_R (0x5c69100000970204)
            float tmp208 = -(gpr9);
            precise float tmp209 = (gpr2 * tmp208);
            gpr4 = tmp209;
            // 007f8 FMUL_R (0x5c69100000572c08)
            float tmp210 = -(gpr5);
            precise float tmp211 = (gpr44 * tmp210);
            gpr8 = tmp211;
            // 00808 FMUL_R (0x5c69100000570312)
            float tmp212 = -(gpr5);
            precise float tmp213 = (gpr3 * tmp212);
            gpr18 = tmp213;
            // 00810 FMUL_R (0x5c68100000570314)
            precise float tmp214 = (gpr3 * gpr5);
            gpr20 = tmp214;
            // 00818 FMUL_R (0x5c68100000570209)
            precise float tmp215 = (gpr2 * gpr5);
            gpr9 = tmp215;
            // 00828 FMUL_R (0x5c6910000057020b)
            float tmp216 = -(gpr5);
            precise float tmp217 = (gpr2 * tmp216);
            gpr11 = tmp217;
            // 00830 MOV_R (0x5c98078002170005)
            gpr5 = gpr33;
            // 00838 MOV_R (0x5c9807800087000a)
            gpr10 = gpr8;
            // 00848 TEXS (0xd98200a1e1e70404)
            gpr256 = texture(sampler1, vec3(gpr4, gpr5, gpr30)).x;
            gpr257 = texture(sampler1, vec3(gpr4, gpr5, gpr30)).y;
            gpr258 = texture(sampler1, vec3(gpr4, gpr5, gpr30)).z;
            gpr4 = gpr256;
            gpr5 = gpr257;
            gpr30 = gpr258;
            // 00850 TEXS (0xd98200a121270808)
            gpr256 = texture(sampler1, vec3(gpr8, gpr9, gpr18)).x;
            gpr257 = texture(sampler1, vec3(gpr8, gpr9, gpr18)).y;
            gpr258 = texture(sampler1, vec3(gpr8, gpr9, gpr18)).z;
            gpr8 = gpr256;
            gpr9 = gpr257;
            gpr18 = gpr258;
            // 00858 TEXS (0xd98200a0e1070e10)
            gpr256 = texture(sampler1, vec3(gpr14, gpr15, gpr16)).x;
            gpr257 = texture(sampler1, vec3(gpr14, gpr15, gpr16)).y;
            gpr258 = texture(sampler1, vec3(gpr14, gpr15, gpr16)).z;
            gpr16 = gpr256;
            gpr17 = gpr257;
            gpr14 = gpr258;
            // 00868 TEXS (0xd98200a0a1470a14)
            gpr256 = texture(sampler1, vec3(gpr10, gpr11, gpr20)).x;
            gpr257 = texture(sampler1, vec3(gpr10, gpr11, gpr20)).y;
            gpr258 = texture(sampler1, vec3(gpr10, gpr11, gpr20)).z;
            gpr20 = gpr256;
            gpr21 = gpr257;
            gpr10 = gpr258;
            // 00870 MOV_R (0x5c98078000f70007)
            gpr7 = gpr15;
            // 00878 TEXS (0xd98200a260c7060c)
            gpr256 = texture(sampler1, vec3(gpr6, gpr7, gpr12)).x;
            gpr257 = texture(sampler1, vec3(gpr6, gpr7, gpr12)).y;
            gpr258 = texture(sampler1, vec3(gpr6, gpr7, gpr12)).z;
            gpr12 = gpr256;
            gpr13 = gpr257;
            gpr38 = gpr258;
            // 00888 FMNMX_R (0x5c60178002a7ff2a)
            gpr42 = utof((!(true) ? ftou(min(utof(0U), gpr42)) : ftou(max(utof(0U), gpr42))));
            // 00890 FMNMX_R (0x5c6017800257ff0f)
            gpr15 = utof((!(true) ? ftou(min(utof(0U), gpr37)) : ftou(max(utof(0U), gpr37))));
            // 00898 FMNMX_R (0x5c6017800187ff18)
            gpr24 = utof((!(true) ? ftou(min(utof(0U), gpr24)) : ftou(max(utof(0U), gpr24))));
            // 008a8 FMNMX_R (0x5c60178001c7ff25)
            gpr37 = utof((!(true) ? ftou(min(utof(0U), gpr28)) : ftou(max(utof(0U), gpr28))));
            // 008b0 FMNMX_R (0x5c6017800167ff16)
            gpr22 = utof((!(true) ? ftou(min(utof(0U), gpr22)) : ftou(max(utof(0U), gpr22))));
            // 008b8 FMNMX_R (0x5c60178001a7ff1a)
            gpr26 = utof((!(true) ? ftou(min(utof(0U), gpr26)) : ftou(max(utof(0U), gpr26))));
            // 008c8 FMUL_C (0x4c68100c00070000)
            precise float tmp218 = (gpr0 * utof(cbuf3[0][0]));
            gpr0 = tmp218;
            // 008d0 FFMA_CR (0x49a0000c01072a00)
            precise float tmp219 = fma(gpr42, utof(cbuf3[4][0]), gpr0);
            gpr0 = tmp219;
            // 008d8 FMUL_C (0x4c68101000872c2a)
            precise float tmp220 = (gpr44 * utof(cbuf4[2][0]));
            gpr42 = tmp220;
            // 008e8 FFMA_CR (0x49a015100097032d)
            precise float tmp221 = fma(gpr3, utof(cbuf4[2][1]), gpr42);
            gpr45 = tmp221;
            // 008f0 FFMA_CR (0x49a0169000a70221)
            precise float tmp222 = fma(gpr2, utof(cbuf4[2][2]), gpr45);
            gpr33 = tmp222;
            // 008f8 FADD_C (0x4c58101000b7212a)
            precise float tmp223 = (gpr33 + utof(cbuf4[2][3]));
            gpr42 = tmp223;
            // 00908 FMUL_C (0x4c68101000472c21)
            precise float tmp224 = (gpr44 * utof(cbuf4[1][0]));
            gpr33 = tmp224;
            // 00910 FFMA_CR (0x49a010900057030b)
            precise float tmp225 = fma(gpr3, utof(cbuf4[1][1]), gpr33);
            gpr11 = tmp225;
            // 00918 FFMA_CR (0x49a0059000670206)
            precise float tmp226 = fma(gpr2, utof(cbuf4[1][2]), gpr11);
            gpr6 = tmp226;
            // 00928 FADD_C (0x4c58101000770606)
            precise float tmp227 = (gpr6 + utof(cbuf4[1][3]));
            gpr6 = tmp227;
            // 00930 FADD_R (0x5c58100002770607)
            precise float tmp228 = (gpr6 + gpr39);
            gpr7 = tmp228;
            // 00938 FMUL_C (0x4c68101000072c06)
            precise float tmp229 = (gpr44 * utof(cbuf4[0][0]));
            gpr6 = tmp229;
            // 00948 FFMA_CR (0x49a0031000170303)
            precise float tmp230 = fma(gpr3, utof(cbuf4[0][1]), gpr6);
            gpr3 = tmp230;
            // 00950 FFMA_CR (0x49a0019000270202)
            precise float tmp231 = fma(gpr2, utof(cbuf4[0][2]), gpr3);
            gpr2 = tmp231;
            // 00958 FADD_R (0x5c58100002972a29)
            precise float tmp232 = (gpr42 + gpr41);
            gpr41 = tmp232;
            // 00968 FFMA_RR (0x59a2158002c72c2a)
            float tmp233 = -(gpr43);
            precise float tmp234 = fma(gpr44, gpr44, tmp233);
            gpr42 = tmp234;
            // 00970 FADD_C (0x4c58101000370202)
            precise float tmp235 = (gpr2 + utof(cbuf4[0][3]));
            gpr2 = tmp235;
            // 00978 FFMA_CR (0x49a00f9001872a06)
            precise float tmp236 = fma(gpr42, utof(cbuf4[6][0]), gpr31);
            gpr6 = tmp236;
            // 00988 FADD_R (0x5c5810000027280b)
            precise float tmp237 = (gpr40 + gpr2);
            gpr11 = tmp237;
            // 00990 FMNMX_R (0x5c6017800247ff02)
            gpr2 = utof((!(true) ? ftou(min(utof(0U), gpr36)) : ftou(max(utof(0U), gpr36))));
            // 00998 FMUL_C (0x4c68100c00072024)
            precise float tmp238 = (gpr32 * utof(cbuf3[0][0]));
            gpr36 = tmp238;
            // 009a8 FFMA_CR (0x49a0149001a72a20)
            precise float tmp239 = fma(gpr42, utof(cbuf4[6][2]), gpr41);
            gpr32 = tmp239;
            // 009b0 FFMA_CR (0x49a00b9001972a1f)
            precise float tmp240 = fma(gpr42, utof(cbuf4[6][1]), gpr23);
            gpr31 = tmp240;
            // 009b8 FFMA_CR (0x49a0099001a72a21)
            precise float tmp241 = fma(gpr42, utof(cbuf4[6][2]), gpr19);
            gpr33 = tmp241;
            // 009c8 FMNMX_R (0x5c6017800237ff13)
            gpr19 = utof((!(true) ? ftou(min(utof(0U), gpr35)) : ftou(max(utof(0U), gpr35))));
            // 009d0 FMNMX_R (0x5c6017800227ff17)
            gpr23 = utof((!(true) ? ftou(min(utof(0U), gpr34)) : ftou(max(utof(0U), gpr34))));
            // 009d8 FFMA_CR (0x49a0059001872a23)
            precise float tmp242 = fma(gpr42, utof(cbuf4[6][0]), gpr11);
            gpr35 = tmp242;
            // 009e8 FFMA_CR (0x49a0039001972a22)
            precise float tmp243 = fma(gpr42, utof(cbuf4[6][1]), gpr7);
            gpr34 = tmp243;
            // 009f0 FMNMX_R (0x5c60178001d7ff2a)
            gpr42 = utof((!(true) ? ftou(min(utof(0U), gpr29)) : ftou(max(utof(0U), gpr29))));
            // 009f8 DEPBAR (0xf0f0000034270000)
            // 00a08 FMUL_C (0x4c68100c00070829)
            precise float tmp244 = (gpr8 * utof(cbuf3[0][0]));
            gpr41 = tmp244;
            // 00a10 FMUL_C (0x4c68100c00071e08)
            precise float tmp245 = (gpr30 * utof(cbuf3[0][0]));
            gpr8 = tmp245;
            // 00a18 FMNMX_R (0x5c6017800207ff1c)
            gpr28 = utof((!(true) ? ftou(min(utof(0U), gpr32)) : ftou(max(utof(0U), gpr32))));
            // 00a28 FMUL_C (0x4c68100c00071220)
            precise float tmp246 = (gpr18 * utof(cbuf3[0][0]));
            gpr32 = tmp246;
            // 00a30 FFMA_CR (0x49a0120c01070202)
            precise float tmp247 = fma(gpr2, utof(cbuf3[4][0]), gpr36);
            gpr2 = tmp247;
            // 00a38 FMNMX_R (0x5c60178001b7ff28)
            gpr40 = utof((!(true) ? ftou(min(utof(0U), gpr27)) : ftou(max(utof(0U), gpr27))));
            // 00a48 FMUL_C (0x4c68100c00071012)
            precise float tmp248 = (gpr16 * utof(cbuf3[0][0]));
            gpr18 = tmp248;
            // 00a50 FMNMX_R (0x5c6017800067ff24)
            gpr36 = utof((!(true) ? ftou(min(utof(0U), gpr6)) : ftou(max(utof(0U), gpr6))));
            // 00a58 FMNMX_R (0x5c6017800227ff1b)
            gpr27 = utof((!(true) ? ftou(min(utof(0U), gpr34)) : ftou(max(utof(0U), gpr34))));
            // 00a68 FMNMX_R (0x5c6017800217ff1d)
            gpr29 = utof((!(true) ? ftou(min(utof(0U), gpr33)) : ftou(max(utof(0U), gpr33))));
            // 00a70 FFMA_CR (0x49a0040c01072a06)
            precise float tmp249 = fma(gpr42, utof(cbuf3[4][0]), gpr8);
            gpr6 = tmp249;
            // 00a78 FMUL_C (0x4c68100c00070101)
            precise float tmp250 = (gpr1 * utof(cbuf3[0][0]));
            gpr1 = tmp250;
            // 00a88 FMUL_C (0x4c68100c00070404)
            precise float tmp251 = (gpr4 * utof(cbuf3[0][0]));
            gpr4 = tmp251;
            // 00a90 FMUL_C (0x4c68100c00070505)
            precise float tmp252 = (gpr5 * utof(cbuf3[0][0]));
            gpr5 = tmp252;
            // 00a98 FMNMX_R (0x5c6017800197ff27)
            gpr39 = utof((!(true) ? ftou(min(utof(0U), gpr25)) : ftou(max(utof(0U), gpr25))));
            // 00aa8 FMNMX_R (0x5c60178001f7ff1e)
            gpr30 = utof((!(true) ? ftou(min(utof(0U), gpr31)) : ftou(max(utof(0U), gpr31))));
            // 00ab0 FFMA_CR (0x49a0148c01071808)
            precise float tmp253 = fma(gpr24, utof(cbuf3[4][0]), gpr41);
            gpr8 = tmp253;
            // 00ab8 FMUL_C (0x4c68100c00070909)
            precise float tmp254 = (gpr9 * utof(cbuf3[0][0]));
            gpr9 = tmp254;
            // 00ac8 DEPBAR (0xf0f0000034170000)
            // 00ad0 FMUL_C (0x4c68100c00070a10)
            precise float tmp255 = (gpr10 * utof(cbuf3[0][0]));
            gpr16 = tmp255;
            // 00ad8 FMUL_C (0x4c68100c00071422)
            precise float tmp256 = (gpr20 * utof(cbuf3[0][0]));
            gpr34 = tmp256;
            // 00ae8 FMUL_C (0x4c68100c00071521)
            precise float tmp257 = (gpr21 * utof(cbuf3[0][0]));
            gpr33 = tmp257;
            // 00af0 FMUL_C (0x4c68100c00070e14)
            precise float tmp258 = (gpr14 * utof(cbuf3[0][0]));
            gpr20 = tmp258;
            // 00af8 MOV32_IMM (0x0103f8000007f003)
            gpr3 = utof(0x3F800000U);
            // 00b08 FMNMX_R (0x5c6017800237ff19)
            gpr25 = utof((!(true) ? ftou(min(utof(0U), gpr35)) : ftou(max(utof(0U), gpr35))));
            // 00b10 FMUL_C (0x4c68100c00071111)
            precise float tmp259 = (gpr17 * utof(cbuf3[0][0]));
            gpr17 = tmp259;
            // 00b18 FMUL_C (0x4c68100c00070c18)
            precise float tmp260 = (gpr12 * utof(cbuf3[0][0]));
            gpr24 = tmp260;
            // 00b28 FMUL_C (0x4c68100c00070d15)
            precise float tmp261 = (gpr13 * utof(cbuf3[0][0]));
            gpr21 = tmp261;
            // 00b30 FMUL_C (0x4c68100c0007261f)
            precise float tmp262 = (gpr38 * utof(cbuf3[0][0]));
            gpr31 = tmp262;
            // 00b38 FFMA_CR (0x49a0080c0107250e)
            precise float tmp263 = fma(gpr37, utof(cbuf3[4][0]), gpr16);
            gpr14 = tmp263;
            // 00b48 FFMA_CR (0x49a0090c01072410)
            precise float tmp264 = fma(gpr36, utof(cbuf3[4][0]), gpr18);
            gpr16 = tmp264;
            // 00b50 FFMA_CR (0x49a0008c01070f01)
            precise float tmp265 = fma(gpr15, utof(cbuf3[4][0]), gpr1);
            gpr1 = tmp265;
            // 00b58 FFMA_CR (0x49a0020c01071304)
            precise float tmp266 = fma(gpr19, utof(cbuf3[4][0]), gpr4);
            gpr4 = tmp266;
            // 00b68 FFMA_CR (0x49a0028c01071705)
            precise float tmp267 = fma(gpr23, utof(cbuf3[4][0]), gpr5);
            gpr5 = tmp267;
            // 00b70 FFMA_CR (0x49a0048c01071609)
            precise float tmp268 = fma(gpr22, utof(cbuf3[4][0]), gpr9);
            gpr9 = tmp268;
            // 00b78 FFMA_CR (0x49a00a0c01071d12)
            precise float tmp269 = fma(gpr29, utof(cbuf3[4][0]), gpr20);
            gpr18 = tmp269;
            // 00b88 MOV_R (0x5c98078000370007)
            gpr7 = gpr3;
            // 00b90 MOV_R (0x5c9807800037000b)
            gpr11 = gpr3;
            // 00b98 MOV_R (0x5c9807800037000f)
            gpr15 = gpr3;
            // 00ba8 MOV_R (0x5c98078000370013)
            gpr19 = gpr3;
            // 00bb0 MOV_R (0x5c98078000370017)
            gpr23 = gpr3;
            // 00bb8 FFMA_CR (0x49a0100c01071a0a)
            precise float tmp270 = fma(gpr26, utof(cbuf3[4][0]), gpr32);
            gpr10 = tmp270;
            // 00bc8 FFMA_CR (0x49a0110c0107280c)
            precise float tmp271 = fma(gpr40, utof(cbuf3[4][0]), gpr34);
            gpr12 = tmp271;
            // 00bd0 FFMA_CR (0x49a0108c0107270d)
            precise float tmp272 = fma(gpr39, utof(cbuf3[4][0]), gpr33);
            gpr13 = tmp272;
            // 00bd8 FFMA_CR (0x49a0088c01071e11)
            precise float tmp273 = fma(gpr30, utof(cbuf3[4][0]), gpr17);
            gpr17 = tmp273;
            // 00be8 FFMA_CR (0x49a00c0c01071914)
            precise float tmp274 = fma(gpr25, utof(cbuf3[4][0]), gpr24);
            gpr20 = tmp274;
            // 00bf0 FFMA_CR (0x49a00a8c01071b15)
            precise float tmp275 = fma(gpr27, utof(cbuf3[4][0]), gpr21);
            gpr21 = tmp275;
            // 00bf8 FFMA_CR (0x49a00f8c01071c16)
            precise float tmp276 = fma(gpr28, utof(cbuf3[4][0]), gpr31);
            gpr22 = tmp276;
            frag_color0.r = gpr0;
            frag_color0.g = gpr1;
            frag_color0.b = gpr2;
            frag_color0.a = gpr3;
            frag_color1.r = gpr4;
            frag_color1.g = gpr5;
            frag_color1.b = gpr6;
            frag_color1.a = gpr7;
            frag_color2.r = gpr8;
            frag_color2.g = gpr9;
            frag_color2.b = gpr10;
            frag_color2.a = gpr11;
            frag_color3.r = gpr12;
            frag_color3.g = gpr13;
            frag_color3.b = gpr14;
            frag_color3.a = gpr15;
            frag_color4.r = gpr16;
            frag_color4.g = gpr17;
            frag_color4.b = gpr18;
            frag_color4.a = gpr19;
            frag_color5.r = gpr20;
            frag_color5.g = gpr21;
            frag_color5.b = gpr22;
            frag_color5.a = gpr23;
            return;
        }
        default: return;
        }
    }
}
