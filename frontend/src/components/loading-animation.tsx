export function LoadingAnimation() {
  return (
    <div className="relative min-h-screen overflow-hidden">
      <div className="pointer-events-none fixed inset-0">
        <div className="absolute inset-0 bg-gradient-to-b from-background via-background/90 to-background" />
        <div className="absolute right-0 top-0 h-[500px] w-[500px] bg-blue-500/10 blur-[100px]" />
        <div className="absolute bottom-0 left-0 h-[500px] w-[500px] bg-purple-500/10 blur-[100px]" />
      </div>

      <div className="absolute inset-0 pointer-events-none">
        <style>{`
          @keyframes wave {
            0%, 100% {
              transform: scale(0.8);
              opacity: 0.1;
            }
            50% {
              transform: scale(1);
              opacity: 0.3;
            }
          }
          .wave-dot {
            animation: wave 3s ease-in-out infinite;
          }
        `}</style>
        <div className="grid gap-6 p-4" style={{
          gridTemplateColumns: 'repeat(auto-fit, minmax(6px, 1fr))',
          minHeight: '100vh',
          width: '100vw'
        }}>
          {Array.from({ length: 800 }, (_, i) => {
            const row = Math.floor(i / 40);
            const col = i % 40;
            const delay = (row * 0.08 + col * 0.03) % 3;
            return (
              <div
                key={i}
                className="w-1.5 h-1.5 bg-blue-500/20 rounded-full wave-dot opacity-0"
                style={{
                  animationDelay: `${delay}s`,
                }}
              />
            );
          })}
        </div>
      </div>

      <div className="relative z-10 flex flex-col justify-center items-center min-h-screen">
        <style>{`
          @keyframes rotate3d {
            0% {
              transform: rotateX(0deg) rotateY(0deg) rotateZ(0deg);
            }
            33% {
              transform: rotateX(120deg) rotateY(120deg) rotateZ(0deg);
            }
            66% {
              transform: rotateX(240deg) rotateY(240deg) rotateZ(120deg);
            }
            100% {
              transform: rotateX(360deg) rotateY(360deg) rotateZ(360deg);
            }
          }
          @keyframes float {
            0%, 100% {
              transform: translateZ(0px);
            }
            50% {
              transform: translateZ(20px);
            }
          }
          @keyframes glow {
            0%, 100% {
              box-shadow: 0 0 20px rgba(59, 130, 246, 0.3);
            }
            50% {
              box-shadow: 0 0 40px rgba(147, 51, 234, 0.6);
            }
          }
          .cube-3d {
            transform-style: preserve-3d;
            animation: rotate3d 8s linear infinite;
          }
          .cube-face {
            position: absolute;
            width: 60px;
            height: 60px;
            background: linear-gradient(45deg, rgba(59, 130, 246, 0.2), rgba(147, 51, 234, 0.2));
            border: 1px solid rgba(59, 130, 246, 0.4);
            animation: glow 3s ease-in-out infinite;
          }
          .cube-face:nth-child(1) { transform: rotateY(0deg) translateZ(30px); }
          .cube-face:nth-child(2) { transform: rotateY(90deg) translateZ(30px); }
          .cube-face:nth-child(3) { transform: rotateY(180deg) translateZ(30px); }
          .cube-face:nth-child(4) { transform: rotateY(-90deg) translateZ(30px); }
          .cube-face:nth-child(5) { transform: rotateX(90deg) translateZ(30px); }
          .cube-face:nth-child(6) { transform: rotateX(-90deg) translateZ(30px); }
          .sphere {
            position: absolute;
            width: 12px;
            height: 12px;
            background: radial-gradient(circle, rgba(59, 130, 246, 0.8), rgba(147, 51, 234, 0.8));
            border-radius: 50%;
            animation: float 2s ease-in-out infinite;
          }
          .sphere:nth-child(1) {
            top: -40px;
            left: 50%;
            transform: translateX(-50%);
            animation-delay: 0s;
          }
          .sphere:nth-child(2) {
            top: 50%;
            right: -40px;
            transform: translateY(-50%);
            animation-delay: 0.5s;
          }
          .sphere:nth-child(3) {
            bottom: -40px;
            left: 50%;
            transform: translateX(-50%);
            animation-delay: 1s;
          }
          .sphere:nth-child(4) {
            top: 50%;
            left: -40px;
            transform: translateY(-50%);
            animation-delay: 1.5s;
          }
        `}</style>
        <div className="text-center space-y-12">
          <div className="relative w-32 h-32 mx-auto" style={{ perspective: '200px' }}>
            <div className="cube-3d relative w-16 h-16 mx-auto">
              <div className="cube-face"></div>
              <div className="cube-face"></div>
              <div className="cube-face"></div>
              <div className="cube-face"></div>
              <div className="cube-face"></div>
              <div className="cube-face"></div>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
