﻿using AuxiliarKinect.FuncoesBasicas;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect.Toolkit.Controls;
using Interacao.Auxiliar;
using AuxiliarKinect.Movimentos.Poses;
using AuxiliarKinect.Movimentos;
using AuxiliarKinect.Movimentos.Gestos;
using AuxiliarKinect.Movimentos.Gestos.Aceno;
using Microsoft.Kinect.Toolkit.Interaction;

namespace Interacao
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        KinectSensor kinect;
        List<IRastreador> rastreadores;
        InteractionStream fluxoInteracao;
        ConfiguracaoDesenho configuracaoMaoDireita;
        ConfiguracaoDesenho configuracaoMaoEsquerda;

        public MainWindow()
        {
            InitializeComponent();
            InicializarSeletor();
            InicializarRastreadores();
            InicializarConfiguracoesDesenho();
        }

        private void InicializarConfiguracoesDesenho()
        {
            configuracaoMaoDireita = new ConfiguracaoDesenho();
            configuracaoMaoDireita.Cor = Brushes.Red;
            configuracaoMaoDireita.Forma = FormaDesenho.Elipse;
            configuracaoMaoDireita.Tamanho = 20;

            configuracaoMaoEsquerda = new ConfiguracaoDesenho();
            configuracaoMaoEsquerda.Cor = Brushes.Blue;
            configuracaoMaoEsquerda.Forma = FormaDesenho.Retangulo;
            configuracaoMaoEsquerda.Tamanho = 20;
        }

        private void InicializarSeletor()
        {
            InicializadorKinect inicializador = new InicializadorKinect();
            inicializador.MetodoInicializadorKinect = InicializarKinect;
            sensorChooserUi.KinectSensorChooser = inicializador.SeletorKinect;
        }

        private void InicializarKinect(KinectSensor kinectSensor)
        {
            kinect = kinectSensor;
            slider.Value = kinect.ElevationAngle;

            kinect.DepthStream.Enable();
            kinect.SkeletonStream.Enable();
            kinect.ColorStream.Enable();
            kinect.AllFramesReady += kinect_AllFramesReady;

            InicializarFluxoInteracao();
        }

        private void InicializarFluxoInteracao()
        {
            fluxoInteracao = new InteractionStream(kinect, canvasDesenho);
            fluxoInteracao.InteractionFrameReady += fluxoInteracao_InteractionFrameReady;
        }

        private void fluxoInteracao_InteractionFrameReady(object sender, InteractionFrameReadyEventArgs e)
        {
            using (InteractionFrame quadro = e.OpenInteractionFrame())
            {
                if (quadro == null) return;

                UserInfo[] informacoesUsuarios = new UserInfo[6];
                quadro.CopyInteractionDataTo(informacoesUsuarios);
                IEnumerable<UserInfo> usuariosRastreados = informacoesUsuarios.Where(info => info.SkeletonTrackingId != 0);
                if (usuariosRastreados.Count() > 0)
                {
                    UserInfo usuarioPrincipal = usuariosRastreados.First();

                    if (usuarioPrincipal.HandPointers[0].HandEventType == InteractionHandEventType.Grip)
                        configuracaoMaoEsquerda.DesenhoAtivo = true;
                    else if (usuarioPrincipal.HandPointers[0].HandEventType == InteractionHandEventType.GripRelease)
                        configuracaoMaoEsquerda.DesenhoAtivo = false;

                    if (usuarioPrincipal.HandPointers[1].HandEventType == InteractionHandEventType.Grip)
                        configuracaoMaoDireita.DesenhoAtivo = true;
                    else if (usuarioPrincipal.HandPointers[1].HandEventType == InteractionHandEventType.GripRelease)
                        configuracaoMaoDireita.DesenhoAtivo = false;

                }
            }
        }

        private void InicializarRastreadores()
        {
            rastreadores = new List<IRastreador>();

            Rastreador<PoseT> rastreadorPoseT = new Rastreador<PoseT>();
            rastreadorPoseT.MovimentoIdentificado += PoseTIdentificada;

            Rastreador<PosePause> rastreadorPosePause = new Rastreador<PosePause>();
            rastreadorPosePause.MovimentoIdentificado += PosePauseIdentificada;
            rastreadorPosePause.MovimentoEmProgresso += PosePauseEmProgresso;

            Rastreador<Aceno> rastreadorAceno = new Rastreador<Aceno>();
            rastreadorAceno.MovimentoIdentificado += AcenoIndentificado;

            rastreadores.Add(rastreadorPoseT);
            rastreadores.Add(rastreadorPosePause);
            rastreadores.Add(rastreadorAceno);
        }


        private void AcenoIndentificado(object sender, EventArgs e)
        {
            if (kinectRegion.KinectSensor == null)
                kinectRegion.KinectSensor = kinect;
        }

        private void PosePauseEmProgresso(object sender, EventArgs e)
        {
            PosePause pose = sender as PosePause;

            Rectangle retangulo = new Rectangle();
            retangulo.Width = canvasKinect.ActualWidth;
            retangulo.Height = 20;
            retangulo.Fill = Brushes.Black;

            Rectangle poseRetangulo = new Rectangle();
            poseRetangulo.Width = canvasKinect.ActualWidth * pose.PercentualProgresso / 100;
            poseRetangulo.Height = 20;
            poseRetangulo.Fill = Brushes.BlueViolet;

            canvasKinect.Children.Add(retangulo);
            canvasKinect.Children.Add(poseRetangulo);
        }

        private void PosePauseIdentificada(object sender, EventArgs e)
        {
            btnEscalaCinza.IsChecked = !btnEscalaCinza.IsChecked;
        }

        private void PoseTIdentificada(object sender, EventArgs e)
        {
            btnEsqueletoUsuario.IsChecked = !btnEsqueletoUsuario.IsChecked;
        }

        private void kinect_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            byte[] imagem = ObterImagemSensorRGB(e.OpenColorImageFrame());

            FuncoesProfundidade(e.OpenDepthImageFrame(), imagem, 2000);

            if (imagem != null)
                canvasKinect.Background = new ImageBrush(BitmapSource.Create(kinect.ColorStream.FrameWidth, kinect.ColorStream.FrameHeight,
                                    96, 96, PixelFormats.Bgr32, null, imagem,
                                    kinect.ColorStream.FrameWidth * kinect.ColorStream.FrameBytesPerPixel));

            canvasKinect.Children.Clear();
            FuncoesEsqueletoUsuario(e.OpenSkeletonFrame());

        }

        private void FuncoesEsqueletoUsuario(SkeletonFrame quadro)
        {
            if (quadro == null) return;

            using (quadro)
            {
                Skeleton esqueletoUsuario = quadro.ObterEsqueletoUsuario();

                if (btnDesenhar.IsChecked)
                {
                    Skeleton[] esqueletos = new Skeleton[6];
                    quadro.CopySkeletonDataTo(esqueletos);
                    fluxoInteracao.ProcessSkeleton(esqueletos, kinect.AccelerometerGetCurrentReading(), quadro.Timestamp);
                    EsqueletoUsuarioAuxiliar esqueletoAuxiliar = new EsqueletoUsuarioAuxiliar(kinect);

                    if (configuracaoMaoDireita.DesenhoAtivo)
                        esqueletoAuxiliar.InteracaoDesenhar(esqueletoUsuario.Joints[JointType.HandRight], canvasDesenho, configuracaoMaoDireita);

                    if (configuracaoMaoEsquerda.DesenhoAtivo)
                        esqueletoAuxiliar.InteracaoDesenhar(esqueletoUsuario.Joints[JointType.HandLeft], canvasDesenho, configuracaoMaoEsquerda);
                }
                else
                {
                    foreach (IRastreador rastreador in rastreadores)
                        rastreador.Rastrear(esqueletoUsuario);

                    if (btnEsqueletoUsuario.IsChecked)
                        quadro.DesenharEsqueletoUsuario(kinect, canvasKinect);
                }
            }
        }

        private byte[] ObterImagemSensorRGB(ColorImageFrame quadro)
        {
            if (quadro == null) return null;

            using (quadro)
            {
                byte[] bytesImagem = new byte[quadro.PixelDataLength];
                quadro.CopyPixelDataTo(bytesImagem);

                return bytesImagem;
            }
        }

        private void FuncoesProfundidade(DepthImageFrame quadro, byte[] bytesImagem, int distanciaMaxima)
        {
            if (quadro == null || bytesImagem == null) return;

            using (quadro)
            {
                DepthImagePixel[] imagemProfundidade = new DepthImagePixel[quadro.PixelDataLength];
                quadro.CopyDepthImagePixelDataTo(imagemProfundidade);

                if (btnDesenhar.IsChecked)
                    fluxoInteracao.ProcessDepth(imagemProfundidade, quadro.Timestamp);
                else if (btnEscalaCinza.IsChecked)
                    ReconhecerProfundidade(bytesImagem, distanciaMaxima, imagemProfundidade);
            }
        }

        private void ReconhecerProfundidade(byte[] bytesImagem, int distanciaMaxima, DepthImagePixel[] imagemProfundidade)
        {
            DepthImagePoint[] pontosImagemProfundidade = new DepthImagePoint[640 * 480];
            kinect.CoordinateMapper.MapColorFrameToDepthFrame(kinect.ColorStream.Format, kinect.DepthStream.Format, imagemProfundidade, pontosImagemProfundidade);

            for (int i = 0; i < pontosImagemProfundidade.Length; i++)
            {
                var point = pontosImagemProfundidade[i];
                if (point.Depth < distanciaMaxima && KinectSensor.IsKnownPoint(point))
                {
                    var pixelDataIndex = i * 4;

                    byte maiorValorCor = Math.Max(bytesImagem[pixelDataIndex], Math.Max(bytesImagem[pixelDataIndex + 1], bytesImagem[pixelDataIndex + 2]));

                    bytesImagem[pixelDataIndex] = maiorValorCor;
                    bytesImagem[pixelDataIndex + 1] = maiorValorCor;
                    bytesImagem[pixelDataIndex + 2] = maiorValorCor;
                }
            }
        }



        private void slider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            kinect.ElevationAngle = Convert.ToInt32(slider.Value);
        }

        private void btnFecharClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void btnDesenharClick(object sender, RoutedEventArgs e)
        {
            if (!btnDesenhar.IsChecked)
                canvasDesenho.Children.Clear();
        }

        private void btnVoltarClick(object sender, RoutedEventArgs e)
        {
            if (kinectRegion.KinectSensor != null)
                kinectRegion.KinectSensor = null;
        }
    }
}
